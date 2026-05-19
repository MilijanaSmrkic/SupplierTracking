namespace SupplierTracking.Domain.Entities;

public class Product
{
    public Guid     Id         { get; private set; }
    public string   Name       { get; private set; } = string.Empty;
    public string   Sku        { get; private set; } = string.Empty;
    public decimal  UnitPrice  { get; private set; }
    public Guid     SupplierId { get; private set; }
    public bool     IsActive   { get; private set; } = true;
    public DateTime CreatedAt  { get; private set; }

    public Supplier? Supplier { get; private set; }

    private Product() { }

    public static Product Create(string name, string sku, decimal unitPrice, Guid supplierId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        return new Product
        {
            Id         = Guid.NewGuid(),
            Name       = name,
            Sku        = sku.ToUpperInvariant(),
            UnitPrice  = unitPrice,
            SupplierId = supplierId,
            IsActive   = true,
            CreatedAt  = DateTime.UtcNow
        };
    }

    public void Update(string name, decimal unitPrice)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be negative.", nameof(unitPrice));

        Name      = name;
        UnitPrice = unitPrice;
    }

    public void Deactivate() => IsActive = false;
}
