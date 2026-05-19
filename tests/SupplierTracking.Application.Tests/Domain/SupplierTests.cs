using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Application.Tests.Domain;

public class SupplierTests
{
    [Fact]
    public void Create_ShouldInitializeWithGeneratedWebhookSecret()
    {
        var supplier = Supplier.Create("ACME Corp", "contact@acme.com", "+1234567890");

        Assert.Equal("ACME Corp", supplier.Name);
        Assert.Equal("contact@acme.com", supplier.ContactEmail);
        Assert.Equal("+1234567890", supplier.ContactPhone);
        Assert.True(supplier.IsActive);
        Assert.NotEmpty(supplier.WebhookSecret);
        Assert.NotEqual(Guid.Empty, supplier.Id);
    }

    [Fact]
    public void Create_ShouldGenerateUniqueWebhookSecrets()
    {
        var s1 = Supplier.Create("Supplier A", "a@test.com");
        var s2 = Supplier.Create("Supplier B", "b@test.com");

        Assert.NotEqual(s1.WebhookSecret, s2.WebhookSecret);
    }

    [Theory]
    [InlineData("", "email@test.com")]
    [InlineData("  ", "email@test.com")]
    [InlineData("Name", "")]
    [InlineData("Name", "  ")]
    public void Create_ShouldThrow_WhenNameOrEmailIsEmpty(string name, string email)
    {
        Assert.Throws<ArgumentException>(() => Supplier.Create(name, email));
    }

    [Fact]
    public void Update_ShouldChangeName()
    {
        var supplier = Supplier.Create("Old Name", "old@test.com");

        supplier.Update("New Name", "new@test.com", null);

        Assert.Equal("New Name", supplier.Name);
        Assert.Equal("new@test.com", supplier.ContactEmail);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var supplier = Supplier.Create("ACME", "acme@test.com");

        supplier.Deactivate();

        Assert.False(supplier.IsActive);
    }

    [Fact]
    public void RegenerateWebhookSecret_ShouldReturnNewSecret()
    {
        var supplier = Supplier.Create("ACME", "acme@test.com");
        var original = supplier.WebhookSecret;

        var newSecret = supplier.RegenerateWebhookSecret();

        Assert.NotEqual(original, newSecret);
        Assert.Equal(newSecret, supplier.WebhookSecret);
    }
}
