using System.Security.Cryptography;
using System.Text;
using SupplierTracking.Application.Abstractions;

namespace SupplierTracking.Infrastructure.Services;

public sealed class WebhookSignatureValidator : IWebhookSignatureValidator
{
    // Expected format: "sha256=<hex>"
    private const string Prefix = "sha256=";

    public bool IsValid(string rawBody, string signature, string webhookSecret)
    {
        if (!signature.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            return false;

        var expected = Compute(rawBody, webhookSecret);
        var received = signature[Prefix.Length..];

        // Constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(received));
    }

    public string Compute(string rawBody, string webhookSecret)
    {
        var key  = Encoding.UTF8.GetBytes(webhookSecret);
        var data = Encoding.UTF8.GetBytes(rawBody);
        var hash = HMACSHA256.HashData(key, data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
