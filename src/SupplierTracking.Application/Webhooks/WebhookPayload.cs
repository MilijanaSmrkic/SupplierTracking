namespace SupplierTracking.Application.Webhooks;

/// <summary>
/// Payload that supplier POSTs to /api/webhooks/supplier/{supplierId}
/// </summary>
public record WebhookPayload(
    string  OrderNumber,
    string  Event,          // "confirmed" | "shipped" | "delivered"
    string? TrackingCode,
    string? Notes);

public static class WebhookEvents
{
    public const string Confirmed = "confirmed";
    public const string Shipped   = "shipped";
    public const string Delivered = "delivered";
}
