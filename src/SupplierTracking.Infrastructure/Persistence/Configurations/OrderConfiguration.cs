using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Infrastructure.Persistence.Configurations;

internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.TrackingCode)
            .HasMaxLength(200);

        builder.Property(o => o.Notes)
            .HasMaxLength(1000);

        builder.HasIndex(o => o.OrderNumber).IsUnique();

        // Optimistic concurrency — SQL Server rowversion (timestamp) column
        builder.Property(o => o.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Composite index — covers the most common paged query: filter by supplier + status, sort by date
        builder.HasIndex(o => new { o.SupplierId, o.Status, o.CreatedAt })
            .IsDescending(false, false, true);

        // Separate index for overdue job: WHERE ExpectedDeliveryDate < NOW AND Status NOT IN (...)
        builder.HasIndex(o => o.ExpectedDeliveryDate);

        // EF Core reads private backing fields for collections
        builder.Navigation(o => o.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(o => o.StatusLogs)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.StatusLogs)
            .WithOne()
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.CreatedBy)
            .WithMany()
            .HasForeignKey(o => o.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
