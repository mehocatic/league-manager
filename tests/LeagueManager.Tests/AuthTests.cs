using LeagueManager.Application.Abstractions;
using LeagueManager.Application.Auth;
using LeagueManager.Application.Common;
using Microsoft.IdentityModel.JsonWebTokens;

namespace LeagueManager.Tests;

public class AuthTests
{
    [Fact]
    public async Task Admin_login_returns_a_token_with_the_admin_role()
    {
        await using var app = await TestApp.CreateAsync();

        var token = (await app.CommandAsync<LoginCommand, Result<AccessToken>>(
            new LoginCommand(TestApp.AdminUsername, TestApp.AdminPassword))).Unwrap();

        var jwt = new JsonWebToken(token.Token);
        Assert.Equal("Admin", jwt.GetClaim("role").Value);
        Assert.Equal(TestApp.AdminUsername, jwt.GetClaim("name").Value);
        Assert.True(token.ExpiresAt > DateTimeOffset.UtcNow);
    }

    [Theory]
    [InlineData(TestApp.AdminUsername, "wrong-password")]
    [InlineData("nobody", TestApp.AdminPassword)]
    public async Task Wrong_password_and_unknown_user_get_the_same_unauthorized_error(string username, string password)
    {
        await using var app = await TestApp.CreateAsync();

        var result = await app.CommandAsync<LoginCommand, Result<AccessToken>>(new LoginCommand(username, password));

        var error = result.Match<Error?>(_ => null, e => e);
        Assert.Equal(Error.Unauthorized("Invalid username or password."), error);
    }
}
