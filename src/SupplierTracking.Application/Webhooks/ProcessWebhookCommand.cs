using MediatR;

namespace SupplierTracking.Application.Webhooks;

public record ProcessWebhookCommand(
    Guid         SupplierId,
    WebhookPayload Payload) : IRequest;
