using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using LeagueManager.Domain.Entities;
using LeagueManager.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LeagueManager.Infrastructure.Ai;

internal sealed class GeminiClient(
    HttpClient http, IOptions<GeminiOptions> options, LeagueDbContext db, ILogger<GeminiClient> logger) : ILlmClient
{
    public async Task<Result<string>> GenerateTextAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        var gemini = options.Value;
        if (string.IsNullOrWhiteSpace(gemini.ApiKey))
        {
            return Error.ExternalService(
                "Gemini API key is not configured. Run: dotnet user-secrets set \"Gemini:ApiKey\" \"<your-key>\" --project src/LeagueManager.Api");
        }

        using var message = new HttpRequestMessage(HttpMethod.Post, $"models/{gemini.Model}:generateContent")
        {
            Content = JsonContent.Create(new
            {
                systemInstruction = new { parts = new[] { new { text = request.SystemInstruction } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = request.Prompt } } } },
                generationConfig = new
                {
                    temperature = gemini.Temperature,
                    maxOutputTokens = gemini.MaxOutputTokens,
                    // A JSON schema forces a bare {"text": ...} reply, without chatter or markdown around it.
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "OBJECT",
                        properties = new { text = new { type = "STRING" } },
                        required = new[] { "text" }
                    }
                }
            })
        };

        // Sent as a header, not as ?key= in the URL, so the key never appears in HttpClient request logs.
        message.Headers.Add("x-goog-api-key", gemini.ApiKey);

        var usage = new AiUsageRecord
        {
            CreatedAtUtc = DateTime.UtcNow,
            Feature = request.Feature,
            Model = gemini.Model,
            MatchId = request.MatchId
        };
        var started = Stopwatch.GetTimestamp();

        try
        {
            using var response = await http.SendAsync(message, cancellationToken);
            usage.StatusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                // Google's error text (e.g. "model not found", "quota exceeded") is what makes a failure diagnosable.
                var details = await response.Content.ReadAsStringAsync(cancellationToken);
                usage.Error = Truncate($"HTTP {(int)response.StatusCode}: {ReadErrorMessage(details)}", 500);
                logger.LogWarning("Gemini request failed: {Error}", usage.Error);
                return Error.ExternalService($"Gemini returned HTTP {(int)response.StatusCode}. Please try again later.");
            }

            var reply = await response.Content.ReadFromJsonAsync<GenerateContentResponse>(cancellationToken);
            if (reply?.UsageMetadata is { } tokens)
            {
                usage.PromptTokens = tokens.PromptTokenCount;
                usage.OutputTokens = tokens.TotalTokenCount - tokens.PromptTokenCount;
            }

            var candidate = reply?.Candidates?.FirstOrDefault();
            var json = candidate?.Content?.Parts?.FirstOrDefault()?.Text;
            if (candidate?.FinishReason != "STOP" || json is null)
            {
                usage.Error = $"Finish reason {candidate?.FinishReason ?? reply?.PromptFeedback?.BlockReason ?? "none"}";
                return Error.ExternalService("Gemini did not return a complete answer.");
            }

            var text = JsonSerializer.Deserialize<TextReply>(json, JsonSerializerOptions.Web)?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                usage.Error = "Empty text";
                return Error.ExternalService("Gemini returned an empty answer.");
            }

            usage.Success = true;
            return text;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            usage.Error = ex.GetType().Name;
            return Error.ExternalService("Gemini could not be reached. Please try again later.");
        }
        finally
        {
            usage.DurationMs = (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
            usage.EstimatedCostUsd =
                (usage.PromptTokens * gemini.InputPricePerMillionTokens + usage.OutputTokens * gemini.OutputPricePerMillionTokens) / 1_000_000;

            // CancellationToken.None: a call that was paid for is logged even if the HTTP request was cancelled.
            db.AiUsageRecords.Add(usage);
            await db.SaveChangesAsync(CancellationToken.None);

            logger.LogInformation(
                "Gemini {Feature} call for match {MatchId}: success={Success}, {PromptTokens} in / {OutputTokens} out tokens, {DurationMs} ms, est. ${Cost:F6}",
                usage.Feature, usage.MatchId, usage.Success, usage.PromptTokens, usage.OutputTokens, usage.DurationMs, usage.EstimatedCostUsd);
        }
    }

    private static string ReadErrorMessage(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<ErrorResponse>(body, JsonSerializerOptions.Web)?.Error?.Message ?? body;
        }
        catch (JsonException)
        {
            return body;
        }
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    private sealed record ErrorResponse(ErrorDetails? Error);
    private sealed record ErrorDetails(string? Message);

    private sealed record GenerateContentResponse(List<Candidate>? Candidates, UsageMetadata? UsageMetadata, PromptFeedback? PromptFeedback);
    private sealed record Candidate(Content? Content, string? FinishReason);
    private sealed record Content(List<Part>? Parts);
    private sealed record Part(string? Text);
    private sealed record UsageMetadata(int PromptTokenCount, int TotalTokenCount);
    private sealed record PromptFeedback(string? BlockReason);
    private sealed record TextReply(string? Text);
}
