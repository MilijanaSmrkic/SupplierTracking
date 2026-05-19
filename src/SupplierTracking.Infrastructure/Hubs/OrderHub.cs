using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SupplierTracking.Infrastructure.Hubs;

[Authorize]
public sealed class OrderHub : Hub
{
    private readonly ILogger<OrderHub> _logger;

    public OrderHub(ILogger<OrderHub> logger) => _logger = logger;

    /// <summary>
    /// Client calls this to receive updates for a specific supplier only.
    /// </summary>
    public async Task JoinSupplierGroup(string supplierId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, SupplierGroup(supplierId));

        _logger.LogDebug(
            "Connection {ConnectionId} joined supplier group {SupplierId}",
            Context.ConnectionId, supplierId);
    }

    /// <summary>
    /// Client calls this to stop receiving supplier-specific updates.
    /// </summary>
    public async Task LeaveSupplierGroup(string supplierId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, SupplierGroup(supplierId));

        _logger.LogDebug(
            "Connection {ConnectionId} left supplier group {SupplierId}",
            Context.ConnectionId, supplierId);
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation(
            "Client connected to OrderHub — ConnectionId={ConnectionId}, User={User}",
            Context.ConnectionId, Context.User?.Identity?.Name);

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation(
            "Client disconnected from OrderHub — ConnectionId={ConnectionId}",
            Context.ConnectionId);

        return base.OnDisconnectedAsync(exception);
    }

    public static string SupplierGroup(string supplierId) => $"supplier-{supplierId}";
}
