using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Store");
        builder.HasKey(store => store.Id);
        builder.Property(store => store.Id).HasColumnName("id");
        builder.Property(store => store.SellerId).HasColumnName("sellerId");
        builder.Property(store => store.StoreName).HasColumnName("storeName").HasMaxLength(100).IsRequired();
        builder.Property(store => store.Description).HasColumnName("description").HasMaxLength(2000);
        builder.Property(store => store.BannerImageUrl).HasColumnName("bannerImageURL").HasMaxLength(2048);
        builder.Property(store => store.SellerType).HasColumnName("sellerType").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(store => store.LegalName).HasColumnName("legalName").HasMaxLength(150).IsRequired();
        builder.Property(store => store.ContactPhone).HasColumnName("contactPhone").HasMaxLength(20).IsRequired();
        builder.Property(store => store.VerificationStatus).HasColumnName("verificationStatus").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(store => store.RejectionReason).HasColumnName("rejectionReason").HasMaxLength(500);
        builder.Property(store => store.ReviewedByUserId).HasColumnName("reviewedByUserId");
        builder.Property(store => store.ReviewedAtUtc).HasColumnName("reviewedAtUtc");
        builder.Property(store => store.CreatedAtUtc).HasColumnName("createdAtUtc");
        builder.Property(store => store.UpdatedAtUtc).HasColumnName("updatedAtUtc");

        builder.HasIndex(store => store.SellerId).IsUnique().HasFilter("[sellerId] IS NOT NULL");
        builder.HasIndex(store => store.StoreName).IsUnique().HasFilter("[storeName] IS NOT NULL");
        builder.HasIndex(store => store.VerificationStatus);
        builder.HasOne(store => store.Seller)
            .WithOne(user => user.Store)
            .HasForeignKey<Store>(store => store.SellerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
