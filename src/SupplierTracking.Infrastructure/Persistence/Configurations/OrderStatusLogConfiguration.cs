using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SupplierTracking.Domain.Entities;

namespace SupplierTracking.Infrastructure.Persistence.Configurations;

internal sealed class OrderStatusLogConfiguration : IEntityTypeConfiguration<OrderStatusLog>
{
    public void Configure(EntityTypeBuilder<OrderStatusLog> builder)
    {
        builder.HasKey(l => l.Id);

        builder.Property(l => l.FromStatus)
            .HasMaxLength(50);

        builder.Property(l => l.ToStatus)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Notes)
            .HasMaxLength(500);

        builder.HasOne(l => l.ChangedBy)
            .WithMany()
            .HasForeignKey(l => l.ChangedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
