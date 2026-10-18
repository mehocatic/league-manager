using System.Security.Cryptography;
using System.Text;
using System.Threading.RateLimiting;
using LeagueManager.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LeagueManager.Api.Authentication;

public static class AuthenticationSetup
{
    public const string LoginRateLimitPolicy = "login";

    public static IServiceCollection AddLeagueAuthentication(
        this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .PostConfigure(options =>
            {
                // No signing key is committed to Git. In Development a random key per process is enough:
                // tokens simply stop working after a restart. Other environments must configure one.
                if (string.IsNullOrEmpty(options.SigningKey) && environment.IsDevelopment())
                {
                    options.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
                }
            })
            .Validate(
                options => Encoding.UTF8.GetByteCount(options.SigningKey) >= JwtOptions.MinSigningKeyBytes,
                $"Jwt:SigningKey must be at least {JwtOptions.MinSigningKeyBytes} bytes. Set it with dotnet user-secrets or the Jwt__SigningKey environment variable.")
            .ValidateOnStart();

        services.AddOptions<AdminOptions>()
            .Bind(configuration.GetSection(AdminOptions.SectionName))
            .Configure(options => options.GeneratePasswordIfMissing = environment.IsDevelopment());

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured through DI so the validator uses the exact same options instance (and dev key) as the token issuer.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwtOptions) =>
            {
                var jwt = jwtOptions.Value;
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = jwt.CreateSigningKey(),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = JwtOptions.NameClaim,
                    RoleClaimType = JwtOptions.RoleClaim
                };
            });

        services.AddAuthorization();

        // Slows down password guessing: 5 login attempts per minute per IP address.
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(LoginRateLimitPolicy, http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 5, Window = TimeSpan.FromMinutes(1) }));
        });

        return services;
    }
}
