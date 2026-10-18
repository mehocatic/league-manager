using LeagueManager.Application.Abstractions;
using LeagueManager.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace LeagueManager.Infrastructure.Authentication;

// Reuses ASP.NET Core Identity's PBKDF2 hasher (random salt per password) without adopting the rest of Identity.
internal sealed class IdentityPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    // The default implementation ignores the user argument.
    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
