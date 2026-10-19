namespace LeagueManager.Infrastructure.Ai;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    // Never in appsettings: comes from dotnet user-secrets or the Gemini__ApiKey environment variable.
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";

    // Flash-Lite, not Flash: on the free tier gemini-3.8-flash allows only 20 requests per day and 5 per minute,
    // which one round (7 summaries plus retries) nearly uses up. Flash-Lite allows 500 per day and 15 per minute.
    public string Model { get; set; } = "gemini-3.5-flash-lite";
    public double Temperature { get; set; } = 0.4;

    // Thinking tokens count against this limit too, so it is far above the ~150 tokens a summary needs.
    public int MaxOutputTokens { get; set; } = 2048;

    // Writing a short text from given facts needs little reasoning; more thinking only adds tokens and seconds
    // (gemini-3.8-flash at its default "medium": about 2000 tokens and 12 s per summary). Empty = model default.
    public string? ThinkingLevel { get; set; } = "minimal";

    // Also the longest rate-limit wait: an admin request should not hang longer than one call is allowed to take.
    public int TimeoutSeconds { get; set; } = 30;

    // Paid-tier list prices in USD; the free tier costs nothing, so the log shows what the same calls would cost.
    public double InputPricePerMillionTokens { get; set; } = 0.30;
    public double OutputPricePerMillionTokens { get; set; } = 2.50;
}
