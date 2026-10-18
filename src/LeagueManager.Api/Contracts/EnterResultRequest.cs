using System.ComponentModel.DataAnnotations;
using LeagueManager.Application.Matches;

namespace LeagueManager.Api.Contracts;

// Nullable + [Required]: with a plain int, a body that forgets a score would silently bind to 0 and record a fake 0:0.
// Goals may be omitted for a 0:0 result.
public sealed record EnterResultRequest(
    [Required] int? HomeGoals,
    [Required] int? AwayGoals,
    IReadOnlyList<GoalInput>? Goals);
