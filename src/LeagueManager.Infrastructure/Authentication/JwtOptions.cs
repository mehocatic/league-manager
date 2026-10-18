using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace LeagueManager.Infrastructure.Authentication;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public const string NameClaim = "name";
    public const string RoleClaim = "role";

    // HMAC-SHA256 needs a key of at least 256 bits.
    public const int MinSigningKeyBytes = 32;

    public string Issuer { get; set; } = "LeagueManager";
    public string Audience { get; set; } = "LeagueManager";
    public string SigningKey { get; set; } = string.Empty;
    public int ExpiresInMinutes { get; set; } = 120;

    public SymmetricSecurityKey CreateSigningKey() => new(Encoding.UTF8.GetBytes(SigningKey));
}
