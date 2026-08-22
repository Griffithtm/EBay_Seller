using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class InventoryConfiguration : IEntityTypeConfiguration<Inventory>
{
    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventory", table => table.HasCheckConstraint("CK_Inventory_Quantity_NonNegative", "[quantity] >= 0"));
        builder.HasKey(inventory => inventory.Id);
        builder.Property(inventory => inventory.Id).HasColumnName("id");
        builder.Property(inventory => inventory.ProductId).HasColumnName("productId");
        builder.Property(inventory => inventory.Quantity).HasColumnName("quantity");
        builder.Property(inventory => inventory.LastUpdatedUtc).HasColumnName("lastUpdated");
        builder.HasIndex(inventory => inventory.ProductId).IsUnique();
        builder.HasOne(inventory => inventory.Product)
            .WithOne(product => product.Inventory)
            .HasForeignKey<Inventory>(inventory => inventory.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
