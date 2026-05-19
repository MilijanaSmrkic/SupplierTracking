namespace SupplierTracking.Domain.Entities;

public class OrderItem
{
    public Guid    Id        { get; private set; }
    public Guid    OrderId   { get; private set; }
    public Guid    ProductId { get; private set; }
    public int     Quantity  { get; private set; }
    public decimal UnitPrice { get; private set; }

    public decimal TotalPrice => Quantity * UnitPrice;

    public Product? Product { get; private set; }

    private OrderItem() { }

    internal static OrderItem Create(Guid orderId, Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        return new OrderItem
        {
            Id        = Guid.NewGuid(),
            OrderId   = orderId,
            ProductId = productId,
            Quantity  = quantity,
            UnitPrice = unitPrice
        };
    }
}
