namespace SupplierTracking.Domain.Entities;

public class User
{
    public int    Id           { get; set; }
    public string UserName     { get; set; } = string.Empty;
    public string Email        { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Role         { get; set; } = UserRoles.Viewer;
    public bool   IsActive     { get; set; } = true;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    // Refresh token — stored hashed, single active token per user
    public string?   RefreshTokenHash   { get; set; }
    public DateTime? RefreshTokenExpiry  { get; set; }
}
