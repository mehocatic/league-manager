using LeagueManager.Application.Common;

namespace LeagueManager.Application.Abstractions;

// Feature and MatchId are only for the usage log.
public sealed record LlmRequest(string Feature, string SystemInstruction, string Prompt, int? MatchId = null);

public interface ILlmClient
{
    Task<Result<string>> GenerateTextAsync(LlmRequest request, CancellationToken cancellationToken);
}
