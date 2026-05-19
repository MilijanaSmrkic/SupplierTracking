using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Abstractions;

public interface ITokenService
{
    string GenerateToken(User user);

    /// <summary>Generates a cryptographically random refresh token (plain text — store hashed).</summary>
    string GenerateRefreshToken();

    /// <summary>Hashes a plain-text refresh token for safe storage.</summary>
    string HashRefreshToken(string refreshToken);

    /// <summary>Verifies a plain-text refresh token against its stored hash.</summary>
    bool   VerifyRefreshToken(string refreshToken, string hash);

    string HashPassword(string password);
    bool   VerifyPassword(string password, string hash);
}
