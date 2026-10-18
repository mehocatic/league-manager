namespace LeagueManager.Infrastructure.Ai;

public sealed class GeminiOptions
{
    public const string SectionName = "Gemini";

    // Never in appsettings: comes from dotnet user-secrets or the Gemini__ApiKey environment variable.
    public string ApiKey { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta/";
    public string Model { get; set; } = "gemini-3.5-flash-lite";
    public double Temperature { get; set; } = 0.4;
    public int MaxOutputTokens { get; set; } = 512;
    public int TimeoutSeconds { get; set; } = 30;

    // Paid-tier list prices in USD; the free tier costs nothing, so the log shows what the same calls would cost.
    public double InputPricePerMillionTokens { get; set; } = 0.30;
    public double OutputPricePerMillionTokens { get; set; } = 2.50;
}
