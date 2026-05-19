using System.ComponentModel.DataAnnotations;

namespace SupplierTracking.Application.Models;

public record SmtpSettings
{
    public const string SectionName = "Smtp";

    public string Host     { get; init; } = string.Empty;

    [Range(1, 65535, ErrorMessage = "Smtp:Port must be between 1 and 65535.")]
    public int    Port     { get; init; } = 587;

    public bool   UseSsl   { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    [MinLength(1, ErrorMessage = "Smtp:From cannot be empty.")]
    public string From     { get; init; } = "noreply@suppliertracking.com";

    public string DigestRecipient { get; init; } = string.Empty;
}
