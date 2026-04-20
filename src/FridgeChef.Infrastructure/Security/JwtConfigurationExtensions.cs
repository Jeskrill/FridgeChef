using System.Text;
using Microsoft.Extensions.Configuration;

namespace FridgeChef.Infrastructure.Security;

public static class JwtConfigurationExtensions
{
    private const int MinimumSecretLengthInBytes = 32;

    public static string GetRequiredJwtSecret(this IConfiguration configuration)
    {
        var secret = configuration["Jwt:Secret"];

        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new InvalidOperationException(
                "Jwt:Secret is not configured. Set it via environment variables or user secrets.");
        }

        if (Encoding.UTF8.GetByteCount(secret) < MinimumSecretLengthInBytes)
        {
            throw new InvalidOperationException(
                "Jwt:Secret must be at least 32 bytes long.");
        }

        return secret;
    }
}
