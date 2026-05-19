namespace SupplierTracking.Application.Models;

public record LoginResponse(
    string   Token,
    string   RefreshToken,
    string   UserName,
    string   Role,
    DateTime ExpiresAt,
    DateTime RefreshTokenExpiresAt);
