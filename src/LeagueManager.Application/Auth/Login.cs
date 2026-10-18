using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace LeagueManager.Application.Auth;

public sealed record LoginCommand(string Username, string Password) : ICommand<Result<AccessToken>>;

internal sealed class LoginCommandHandler(ILeagueDbContext db, IPasswordHasher passwordHasher, IAccessTokenIssuer tokenIssuer)
    : ICommandHandler<LoginCommand, Result<AccessToken>>
{
    public async Task<Result<AccessToken>> HandleAsync(LoginCommand command, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Username == command.Username, cancellationToken);

        // One message for both cases, so the response does not reveal which usernames exist.
        if (user is null || !passwordHasher.Verify(user.PasswordHash, command.Password))
        {
            return Error.Unauthorized("Invalid username or password.");
        }

        return tokenIssuer.Issue(user);
    }
}
