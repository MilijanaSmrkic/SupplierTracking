namespace SupplierTracking.Domain.Entities;

public class Order
{
    private readonly List<OrderItem>      _items      = [];
    private readonly List<OrderStatusLog> _statusLogs = [];

    public Guid      Id                   { get; private set; }
    public string    OrderNumber          { get; private set; } = string.Empty;
    public Guid      SupplierId           { get; private set; }
    public int       CreatedByUserId      { get; private set; }
    public string    Status               { get; private set; } = OrderStatuses.Draft;
    public DateTime? ExpectedDeliveryDate { get; private set; }
    public string?   TrackingCode         { get; private set; }
    public string?   Notes                { get; private set; }
    public DateTime  CreatedAt            { get; private set; }
    public DateTime  UpdatedAt            { get; private set; }

    /// <summary>
    /// Optimistic concurrency token — EF Core uses this to detect conflicting updates.
    /// If two requests load the same order and both try to save, the second one gets
    /// DbUpdateConcurrencyException instead of silently overwriting the first.
    /// </summary>
    public byte[] RowVersion { get; private set; } = [];

    public decimal TotalAmount => _items.Sum(i => i.TotalPrice);

    public Supplier?                         Supplier   { get; private set; }
    public User?                             CreatedBy  { get; private set; }
    public IReadOnlyCollection<OrderItem>    Items      => _items.AsReadOnly();
    public IReadOnlyCollection<OrderStatusLog> StatusLogs => _statusLogs.AsReadOnly();

    private Order() { }

    public static Order Create(
        Guid supplierId,
        int createdByUserId,
        DateTime? expectedDeliveryDate = null,
        string? notes = null)
    {
        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id                   = Guid.NewGuid(),
            OrderNumber          = GenerateOrderNumber(now),
            SupplierId           = supplierId,
            CreatedByUserId      = createdByUserId,
            Status               = OrderStatuses.Draft,
            ExpectedDeliveryDate = expectedDeliveryDate,
            Notes                = notes,
            CreatedAt            = now,
            UpdatedAt            = now
        };

        order._statusLogs.Add(OrderStatusLog.Create(
            order.Id, null, OrderStatuses.Draft, createdByUserId));

        return order;
    }

    public void AddItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (Status != OrderStatuses.Draft)
            throw new InvalidOperationException("Items can only be added to orders in Draft status.");

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentException("Unit price must be greater than zero.", nameof(unitPrice));

        _items.Add(OrderItem.Create(Id, productId, quantity, unitPrice));
        UpdatedAt = DateTime.UtcNow;
    }

    public void Send(int userId)
    {
        EnsureStatus(OrderStatuses.Draft, "send");

        Transition(OrderStatuses.Sent, userId);
    }

    public void Confirm(int? userId = null, string? notes = null)
    {
        EnsureStatus(OrderStatuses.Sent, "confirm");

        Transition(OrderStatuses.Confirmed, userId, notes);
    }

    public void MarkInTransit(string? trackingCode = null, int? userId = null)
    {
        if (Status != OrderStatuses.Confirmed)
            throw new InvalidOperationException(
                $"Order must be Confirmed before it can be marked InTransit. Current status: {Status}.");

        TrackingCode = trackingCode;
        Transition(OrderStatuses.InTransit, userId, trackingCode is not null ? $"Tracking: {trackingCode}" : null);
    }

    public void MarkDelivered(int? userId = null)
    {
        EnsureStatus(OrderStatuses.InTransit, "deliver");

        Transition(OrderStatuses.Delivered, userId);
    }

    public void Cancel(int userId, string? reason = null)
    {
        if (Status == OrderStatuses.Delivered || Status == OrderStatuses.Cancelled)
            throw new InvalidOperationException(
                $"Order cannot be cancelled from status '{Status}'.");

        Transition(OrderStatuses.Cancelled, userId, reason);
    }

    public void UpdateExpectedDelivery(DateTime expectedDeliveryDate)
    {
        if (Status is OrderStatuses.Delivered or OrderStatuses.Cancelled)
            throw new InvalidOperationException("Cannot update delivery date on a completed order.");

        ExpectedDeliveryDate = expectedDeliveryDate;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsOverdue =>
        ExpectedDeliveryDate.HasValue &&
        DateTime.UtcNow > ExpectedDeliveryDate.Value &&
        Status is not OrderStatuses.Delivered and not OrderStatuses.Cancelled;

    private void Transition(string toStatus, int? userId = null, string? notes = null)
    {
        _statusLogs.Add(OrderStatusLog.Create(Id, Status, toStatus, userId, notes));
        Status    = toStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    private void EnsureStatus(string required, string action)
    {
        if (Status != required)
            throw new InvalidOperationException(
                $"Cannot {action} an order with status '{Status}'. Required status: '{required}'.");
    }

    private static string GenerateOrderNumber(DateTime now) =>
        $"ORD-{now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
}
