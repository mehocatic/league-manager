using System.Net;
using System.Text;
using System.Text.Json;
using LeagueManager.Application.Abstractions;
using LeagueManager.Application.AiUsage;
using LeagueManager.Application.Common;
using LeagueManager.Application.Matches;
using LeagueManager.Infrastructure.Ai;
using Microsoft.Extensions.DependencyInjection;

namespace LeagueManager.Tests;

// Runs the real GeminiClient against a fake HTTP handler, so request format and rate-limit handling are tested offline.
public class GeminiClientTests
{
    private const string ApiKey = "test-key";

    // A response function returning null means "never answer", which lets the HttpClient timeout fire.
    private sealed class FakeGemini(params Func<string, HttpResponseMessage?>[] responses) : HttpMessageHandler
    {
        public List<(HttpRequestMessage Request, string Body)> Calls { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = await request.Content!.ReadAsStringAsync(cancellationToken);
            Calls.Add((request, body));

            var response = responses[Calls.Count - 1](body);
            if (response is null)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            return response!;
        }
    }

    private static HttpResponseMessage? NeverAnswers(string _) => null;

    private static HttpResponseMessage RateLimited(string retryDelay) => new(HttpStatusCode.TooManyRequests)
    {
        Content = new StringContent(
            $$$"""{"error":{"code":429,"message":"Quota exceeded","details":[{"@type":"type.googleapis.com/google.rpc.RetryInfo","retryDelay":"{{{retryDelay}}}"}]}}""",
            Encoding.UTF8, "application/json")
    };

    private static HttpResponseMessage ValidAnswer(string requestBody)
    {
        var prompt = JsonDocument.Parse(requestBody).RootElement.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString()!;
        var summary = FakeLlmClient.ValidSummaryFromPrompt(new LlmRequest("", "", prompt)).Unwrap();
        var reply = new
        {
            candidates = new[] { new { content = new { parts = new[] { new { text = JsonSerializer.Serialize(new { text = summary }) } } }, finishReason = "STOP" } },
            usageMetadata = new { promptTokenCount = 100, totalTokenCount = 150 }
        };
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(reply), Encoding.UTF8, "application/json") };
    }

    private static Task<TestApp> CreateAppAsync(FakeGemini gemini, int timeoutSeconds = 5) =>
        TestApp.CreateAsync(services =>
        {
            services.Configure<GeminiOptions>(o => (o.ApiKey, o.TimeoutSeconds) = (ApiKey, timeoutSeconds));
            // AddHttpClient<ILlmClient, GeminiClient> names the client after the interface.
            services.AddHttpClient(nameof(ILlmClient)).ConfigurePrimaryHttpMessageHandler(() => gemini);
        });

    [Fact]
    public async Task Short_rate_limit_is_waited_out_and_both_calls_are_logged()
    {
        var gemini = new FakeGemini(_ => RateLimited("0.2s"), ValidAnswer);
        await using var app = await CreateAppAsync(gemini);

        var match = (await app.CommandAsync<GenerateMatchSummaryCommand, Result<MatchDetailsDto>>(new GenerateMatchSummaryCommand(1))).Unwrap();

        Assert.NotNull(match.Summary);
        Assert.Equal(2, gemini.Calls.Count);

        var usage = await app.QueryAsync<GetAiUsageQuery, AiUsageReportDto>(new GetAiUsageQuery());
        Assert.Equal((2, 1), (usage.Calls, usage.FailedCalls));
        Assert.Equal("Rate limit (HTTP 429), retry in 1 s: Quota exceeded", usage.Recent[^1].Error);
    }

    [Fact]
    public async Task Rate_limit_longer_than_the_timeout_is_reported_instead_of_waited_for()
    {
        var gemini = new FakeGemini(_ => RateLimited("33s"));
        await using var app = await CreateAppAsync(gemini, timeoutSeconds: 30);

        var result = await app.CommandAsync<GenerateMatchSummaryCommand, Result<MatchDetailsDto>>(new GenerateMatchSummaryCommand(1));

        Assert.Equal(Error.ExternalService("Gemini rate limit reached, try again in 33 s."), result.Match<Error?>(_ => null, e => e));
        Assert.Single(gemini.Calls);
    }

    [Fact]
    public async Task Timeout_is_reported_and_logged_as_a_timeout()
    {
        var gemini = new FakeGemini(NeverAnswers);
        await using var app = await CreateAppAsync(gemini, timeoutSeconds: 1);

        var result = await app.CommandAsync<GenerateMatchSummaryCommand, Result<MatchDetailsDto>>(new GenerateMatchSummaryCommand(1));

        Assert.Equal(Error.ExternalService("Gemini did not answer within 1 s. Please try again later."), result.Match<Error?>(_ => null, e => e));
        var usage = await app.QueryAsync<GetAiUsageQuery, AiUsageReportDto>(new GetAiUsageQuery());
        Assert.Equal("Timeout: no answer within 1 s", Assert.Single(usage.Recent).Error);
    }

    [Fact]
    public async Task Request_sends_the_key_as_a_header_and_asks_for_minimal_thinking()
    {
        var gemini = new FakeGemini(ValidAnswer);
        await using var app = await CreateAppAsync(gemini);

        await app.CommandAsync<GenerateMatchSummaryCommand, Result<MatchDetailsDto>>(new GenerateMatchSummaryCommand(1));

        var (request, body) = Assert.Single(gemini.Calls);
        Assert.Equal(ApiKey, Assert.Single(request.Headers.GetValues("x-goog-api-key")));
        Assert.DoesNotContain(ApiKey, request.RequestUri!.ToString());

        var config = JsonDocument.Parse(body).RootElement.GetProperty("generationConfig");
        Assert.Equal("minimal", config.GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString());
        Assert.Equal("application/json", config.GetProperty("responseMimeType").GetString());
    }
}
