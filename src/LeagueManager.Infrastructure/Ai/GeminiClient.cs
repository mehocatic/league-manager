using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
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
    // Optional settings that are not configured are left out of the request instead of being sent as null.
    private static readonly JsonSerializerOptions RequestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<Result<string>> GenerateTextAsync(LlmRequest request, CancellationToken cancellationToken)
    {
        var gemini = options.Value;
        if (string.IsNullOrWhiteSpace(gemini.ApiKey))
        {
            return Error.ExternalService(
                "Gemini API key is not configured. Run: dotnet user-secrets set \"Gemini:ApiKey\" \"<your-key>\" --project src/LeagueManager.Api");
        }

        var (result, retryAfter) = await SendOnceAsync(request, gemini, cancellationToken);

        // The free tier allows only a few requests per minute, so a full round can hit the limit. Gemini says how long
        // to wait; a short wait is worth it, a long one (or a used-up daily quota) is reported instead of waited out.
        if (retryAfter is { } delay)
        {
            if (delay > TimeSpan.FromSeconds(gemini.TimeoutSeconds))
            {
                return Error.ExternalService($"Gemini rate limit reached, try again in {Math.Ceiling(delay.TotalSeconds):F0} s.");
            }

            logger.LogInformation("Gemini rate limit reached, retrying in {Seconds:F0} s", delay.TotalSeconds);
            await Task.Delay(delay, cancellationToken);
            (result, _) = await SendOnceAsync(request, gemini, cancellationToken);
        }

        return result;
    }

    private async Task<(Result<string> Result, TimeSpan? RetryAfter)> SendOnceAsync(
        LlmRequest request, GeminiOptions gemini, CancellationToken cancellationToken)
    {
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
                    thinkingConfig = string.IsNullOrWhiteSpace(gemini.ThinkingLevel) ? null : new { thinkingLevel = gemini.ThinkingLevel },
                    // A JSON schema forces a bare {"text": ...} reply, without chatter or markdown around it.
                    responseMimeType = "application/json",
                    responseSchema = new
                    {
                        type = "OBJECT",
                        properties = new { text = new { type = "STRING" } },
                        required = new[] { "text" }
                    }
                }
            }, options: RequestJsonOptions)
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
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var error = ReadError(body);

                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = response.Headers.RetryAfter?.Delta ?? ReadRetryDelay(error);
                    var retryText = retryAfter is { } wait ? $", retry in {Math.Ceiling(wait.TotalSeconds):F0} s" : "";
                    usage.Error = Truncate($"Rate limit (HTTP 429){retryText}: {error?.Message ?? body}", 500);
                    logger.LogWarning("Gemini rate limit reached: {Error}", usage.Error);
                    return (Error.ExternalService("Gemini rate limit reached. Please try again later."), retryAfter);
                }

                usage.Error = Truncate($"HTTP {(int)response.StatusCode}: {error?.Message ?? body}", 500);
                logger.LogWarning("Gemini request failed: {Error}", usage.Error);
                return (Error.ExternalService($"Gemini returned HTTP {(int)response.StatusCode}. Please try again later."), null);
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
                return (Error.ExternalService("Gemini did not return a complete answer."), null);
            }

            var text = JsonSerializer.Deserialize<TextReply>(json, JsonSerializerOptions.Web)?.Text;
            if (string.IsNullOrWhiteSpace(text))
            {
                usage.Error = "Empty text";
                return (Error.ExternalService("Gemini returned an empty answer."), null);
            }

            usage.Success = true;
            return (text, null);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient reports its own timeout as a cancellation; the caller did not cancel, so it is a timeout.
            usage.Error = $"Timeout: no answer within {gemini.TimeoutSeconds} s";
            logger.LogWarning("Gemini did not answer within {Seconds} s", gemini.TimeoutSeconds);
            return (Error.ExternalService($"Gemini did not answer within {gemini.TimeoutSeconds} s. Please try again later."), null);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            usage.Error = ex.GetType().Name;
            return (Error.ExternalService("Gemini could not be reached. Please try again later."), null);
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

    private static ErrorDetails? ReadError(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<ErrorResponse>(body, JsonSerializerOptions.Web)?.Error;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // A 429 body carries a google.rpc.RetryInfo detail such as { "retryDelay": "33s" }.
    private static TimeSpan? ReadRetryDelay(ErrorDetails? error)
    {
        var delay = error?.Details?
            .Where(d => d.ValueKind == JsonValueKind.Object && d.TryGetProperty("retryDelay", out _))
            .Select(d => d.GetProperty("retryDelay").GetString())
            .FirstOrDefault();

        return delay is not null && double.TryParse(delay.TrimEnd('s'), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
            ? TimeSpan.FromSeconds(seconds)
            : null;
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];

    private sealed record ErrorResponse(ErrorDetails? Error);
    private sealed record ErrorDetails(string? Message, List<JsonElement>? Details);

    private sealed record GenerateContentResponse(List<Candidate>? Candidates, UsageMetadata? UsageMetadata, PromptFeedback? PromptFeedback);
    private sealed record Candidate(Content? Content, string? FinishReason);
    private sealed record Content(List<Part>? Parts);
    private sealed record Part(string? Text);
    private sealed record UsageMetadata(int PromptTokenCount, int TotalTokenCount);
    private sealed record PromptFeedback(string? BlockReason);
    private sealed record TextReply(string? Text);
}
