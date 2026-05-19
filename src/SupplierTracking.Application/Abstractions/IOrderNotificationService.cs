using SupplierTracking.Application.Orders.Events;

namespace SupplierTracking.Application.Abstractions;

public interface IOrderNotificationService
{
    /// <summary>
    /// Broadcasts order status change to all connected clients and
    /// to the supplier-specific group.
    /// </summary>
    Task NotifyOrderStatusChangedAsync(
        OrderStatusChangedEvent notification,
        CancellationToken cancellationToken = default);
}
