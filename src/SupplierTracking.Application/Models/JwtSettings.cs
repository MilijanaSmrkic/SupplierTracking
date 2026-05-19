using System.ComponentModel.DataAnnotations;

namespace SupplierTracking.Application.Models;

public record JwtSettings
{
    public const string SectionName = "JwtSettings";

    [MinLength(32, ErrorMessage = "JwtSettings:SecretKey must be at least 32 characters.")]
    public string SecretKey      { get; init; } = string.Empty;

    [MinLength(1, ErrorMessage = "JwtSettings:Issuer cannot be empty.")]
    public string Issuer         { get; init; } = string.Empty;

    [MinLength(1, ErrorMessage = "JwtSettings:Audience cannot be empty.")]
    public string Audience       { get; init; } = string.Empty;

    [Range(1, 10080, ErrorMessage = "JwtSettings:ExpiryMinutes must be between 1 and 10080.")]
    public int    ExpiryMinutes  { get; init; } = 60;
}
