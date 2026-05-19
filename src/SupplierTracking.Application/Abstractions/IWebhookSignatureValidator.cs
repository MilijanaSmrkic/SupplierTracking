namespace SupplierTracking.Application.Abstractions;

public interface IWebhookSignatureValidator
{
    /// <summary>
    /// Validates that the X-Supplier-Signature header matches
    /// HMAC-SHA256(rawBody, webhookSecret).
    /// </summary>
    bool IsValid(string rawBody, string signature, string webhookSecret);

    /// <summary>
    /// Computes the expected signature for a given body and secret.
    /// Useful for testing / generating signatures.
    /// </summary>
    string Compute(string rawBody, string webhookSecret);
}
