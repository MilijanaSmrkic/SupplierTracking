namespace SupplierTracking.Domain.Entities;

public class Supplier
{
    public Guid   Id             { get; private set; }
    public string Name           { get; private set; } = string.Empty;
    public string ContactEmail   { get; private set; } = string.Empty;
    public string? ContactPhone  { get; private set; }
    public string WebhookSecret  { get; private set; } = string.Empty;
    public bool   IsActive       { get; private set; } = true;
    public DateTime CreatedAt    { get; private set; }

    public IReadOnlyCollection<Product> Products { get; private set; } = [];
    public IReadOnlyCollection<Order>   Orders   { get; private set; } = [];

    private Supplier() { }

    public static Supplier Create(string name, string contactEmail, string? contactPhone = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactEmail);

        return new Supplier
        {
            Id            = Guid.NewGuid(),
            Name          = name,
            ContactEmail  = contactEmail,
            ContactPhone  = contactPhone,
            WebhookSecret = GenerateWebhookSecret(),
            IsActive      = true,
            CreatedAt     = DateTime.UtcNow
        };
    }

    public void Update(string name, string contactEmail, string? contactPhone)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(contactEmail);

        Name         = name;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
    }

    public void Deactivate() => IsActive = false;

    public string RegenerateWebhookSecret()
    {
        WebhookSecret = GenerateWebhookSecret();
        return WebhookSecret;
    }

    private static string GenerateWebhookSecret() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}
