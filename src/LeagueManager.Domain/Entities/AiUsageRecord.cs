namespace LeagueManager.Domain.Entities;

// One row per call to the LLM API, including failed calls, so usage and cost can be audited.
public class AiUsageRecord
{
    public int Id { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string Feature { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? MatchId { get; set; }

    public bool Success { get; set; }
    public int? StatusCode { get; set; }
    public string? Error { get; set; }

    public int PromptTokens { get; set; }

    // Includes "thinking" tokens, which are billed as output.
    public int OutputTokens { get; set; }
    public int DurationMs { get; set; }

    // double, not decimal: SQLite cannot SUM decimal columns, and this is an estimate anyway.
    public double EstimatedCostUsd { get; set; }
}
