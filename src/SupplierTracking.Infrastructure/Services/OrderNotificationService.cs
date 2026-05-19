using Microsoft.AspNetCore.SignalR;
using SupplierTracking.Application.Abstractions;
using SupplierTracking.Application.Orders.Events;
using SupplierTracking.Infrastructure.Hubs;

namespace SupplierTracking.Infrastructure.Services;

internal sealed class OrderNotificationService : IOrderNotificationService
{
    private readonly IHubContext<OrderHub> _hubContext;

    public OrderNotificationService(IHubContext<OrderHub> hubContext) =>
        _hubContext = hubContext;

    public async Task NotifyOrderStatusChangedAsync(
        OrderStatusChangedEvent notification,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            notification.OrderId,
            notification.OrderNumber,
            notification.SupplierId,
            notification.SupplierName,
            notification.FromStatus,
            notification.ToStatus,
            notification.ChangedByUserId,
            notification.ChangedAt
        };

        // Notify all connected clients (dashboard)
        await _hubContext.Clients.All
            .SendAsync("OrderStatusChanged", payload, cancellationToken);

        // Also notify supplier-specific group
        await _hubContext.Clients
            .Group(OrderHub.SupplierGroup(notification.SupplierId.ToString()))
            .SendAsync("OrderStatusChanged", payload, cancellationToken);
    }
}
