using LeagueManager.Domain.Entities;

namespace LeagueManager.Application.Abstractions;

public sealed record AccessToken(string Token, DateTimeOffset ExpiresAt);

public interface IAccessTokenIssuer
{
    AccessToken Issue(User user);
}
