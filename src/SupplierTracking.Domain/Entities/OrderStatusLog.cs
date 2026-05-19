namespace SupplierTracking.Domain.Entities;

public class OrderStatusLog
{
    public Guid      Id              { get; private set; }
    public Guid      OrderId         { get; private set; }
    public string?   FromStatus      { get; private set; }
    public string    ToStatus        { get; private set; } = string.Empty;
    public int?      ChangedByUserId { get; private set; }
    public DateTime  ChangedAt       { get; private set; }
    public string?   Notes           { get; private set; }

    public User? ChangedBy { get; private set; }

    private OrderStatusLog() { }

    internal static OrderStatusLog Create(
        Guid orderId,
        string? fromStatus,
        string toStatus,
        int? changedByUserId = null,
        string? notes = null)
    {
        return new OrderStatusLog
        {
            Id              = Guid.NewGuid(),
            OrderId         = orderId,
            FromStatus      = fromStatus,
            ToStatus        = toStatus,
            ChangedByUserId = changedByUserId,
            ChangedAt       = DateTime.UtcNow,
            Notes           = notes
        };
    }
}
