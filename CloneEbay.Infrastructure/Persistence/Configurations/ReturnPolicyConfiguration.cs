using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class ReturnPolicyConfiguration : IEntityTypeConfiguration<ReturnPolicy>
{
    public void Configure(EntityTypeBuilder<ReturnPolicy> builder)
    {
        builder.ToTable("ReturnPolicy");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.StoreId).HasColumnName("storeId");
        builder.Property(p => p.AcceptReturns).HasColumnName("acceptReturns");
        builder.Property(p => p.ReturnPeriodDays).HasColumnName("returnPeriodDays");
        builder.Property(p => p.ReturnShippingPaidBy).HasColumnName("returnShippingPaidBy").HasMaxLength(20);
        builder.Property(p => p.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(p => p.CreatedAt).HasColumnName("createdAt");
        builder.Property(p => p.UpdatedAt).HasColumnName("updatedAt");

        builder.HasOne(p => p.Store)
            .WithMany()
            .HasForeignKey(p => p.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.StoreId).IsUnique();
    }
}
