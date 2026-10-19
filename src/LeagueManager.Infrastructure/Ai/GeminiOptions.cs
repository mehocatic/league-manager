namespace LeagueManager.Infrastructure.Ai;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    // Never in appsettings: comes from dotnet user-secrets or the Gemini__ApiKey environment variable.
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
    public string Model { get; set; } = "gemini-3.8-flash";
    public double Temperature { get; set; } = 0.4;

    // Thinking tokens count against this limit too, so it is far above the ~150 tokens a summary needs.
    public int MaxOutputTokens { get; set; } = 2048;

    // Flash models think at "medium" by default: about 2000 extra tokens and 12 s per summary. Writing a short text
    // from given facts needs little reasoning. Empty = model default (for models without thinking levels).
    public string? ThinkingLevel { get; set; } = "low";
    public int TimeoutSeconds { get; set; } = 30;

    // Longest wait for a rate-limit retry; a longer requested wait means the daily quota is used up, so give up.
    public int MaxRateLimitWaitSeconds { get; set; } = 60;

    // Paid-tier list prices in USD; the free tier costs nothing, so the log shows what the same calls would cost.
    public double InputPricePerMillionTokens { get; set; } = 0.75;
    public double OutputPricePerMillionTokens { get; set; } = 3.75;
}
