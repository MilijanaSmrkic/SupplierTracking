using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Abstractions.Repositories;
using SupplierTracking.Application.Webhooks;

namespace SupplierTracking.Api.Controllers;

/// <summary>
/// Inbound webhook endpoint for suppliers to push order status updates.
/// All requests must be signed with HMAC-SHA256 using the supplier's webhook secret.
/// </summary>
[ApiController]
[Route("api/webhooks")]
[Produces("application/json")]
public sealed class WebhookController : ControllerBase
{
    private const string SignatureHeader = "X-Supplier-Signature";

    private readonly IMediator                  _mediator;
    private readonly ISupplierRepository        _supplierRepository;
    private readonly IWebhookSignatureValidator _validator;

    /// <inheritdoc />
    public WebhookController(
        IMediator mediator,
        ISupplierRepository supplierRepository,
        IWebhookSignatureValidator validator)
    {
        _mediator           = mediator;
        _supplierRepository = supplierRepository;
        _validator          = validator;
    }

    /// <summary>
    /// Receive an order status update from a supplier.
    /// </summary>
    /// <remarks>
    /// The supplier must include an HMAC-SHA256 signature in the <c>X-Supplier-Signature</c> header:
    ///
    ///     X-Supplier-Signature: sha256={hex_encoded_hmac}
    ///
    /// The HMAC is computed over the raw request body using the supplier's webhook secret as the key.
    ///
    /// Supported event types:
    ///
    /// | Event       | Transition              |
    /// |-------------|-------------------------|
    /// | confirmed   | Sent → Confirmed        |
    /// | shipped     | Confirmed → InTransit   |
    /// | delivered   | InTransit → Delivered   |
    ///
    /// Example payload:
    ///
    ///     {
    ///         "orderNumber": "ORD-20240101-ABC123",
    ///         "event": "shipped",
    ///         "trackingCode": "DHL-999888",
    ///         "notes": null
    ///     }
    /// </remarks>
    /// <param name="supplierId">The supplier's GUID (from the URL path).</param>
    /// <response code="200">Webhook processed successfully.</response>
    /// <response code="400">Invalid JSON, missing required fields, or unknown event type.</response>
    /// <response code="401">Missing or invalid <c>X-Supplier-Signature</c> header.</response>
    /// <response code="404">Supplier not found or inactive.</response>
    [HttpPost("supplier/{supplierId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReceiveWebhook(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        // 1. Read raw body — needed for HMAC before deserialization
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        // 2. Validate that signature header exists
        if (!Request.Headers.TryGetValue(SignatureHeader, out var signatureValues) ||
            string.IsNullOrWhiteSpace(signatureValues))
        {
            return Unauthorized(new { message = $"Missing {SignatureHeader} header." });
        }

        // 3. Load supplier to get their webhook secret
        var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken);
        if (supplier is null || !supplier.IsActive)
            return NotFound(new { message = $"Supplier {supplierId} not found." });

        // 4. Validate HMAC signature
        var signature = signatureValues.ToString();
        if (!_validator.IsValid(rawBody, signature, supplier.WebhookSecret))
        {
            return Unauthorized(new { message = "Invalid webhook signature." });
        }

        // 5. Deserialize payload
        WebhookPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<WebhookPayload>(rawBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest(new { message = "Invalid JSON payload." });
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.OrderNumber) ||
            string.IsNullOrWhiteSpace(payload.Event))
        {
            return BadRequest(new { message = "OrderNumber and Event are required." });
        }

        // 6. Dispatch to handler
        await _mediator.Send(new ProcessWebhookCommand(supplierId, payload), cancellationToken);

        return Ok(new { message = "Webhook processed successfully." });
    }
}
