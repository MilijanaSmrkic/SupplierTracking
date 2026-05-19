namespace SupplierTracking.Application.Models;

public record LoginResponse(
    string   Token,
    string   UserName,
    string   Role,
    DateTime ExpiresAt);
