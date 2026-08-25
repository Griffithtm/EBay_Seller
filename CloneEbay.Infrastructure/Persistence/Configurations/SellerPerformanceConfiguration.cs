using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class SellerPerformanceConfiguration : IEntityTypeConfiguration<SellerPerformance>
{
    public void Configure(EntityTypeBuilder<SellerPerformance> builder)
    {
        builder.ToTable("SellerPerformance");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");
        builder.Property(p => p.SellerId).HasColumnName("sellerId");
        builder.Property(p => p.Level).HasColumnName("level").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.TransactionDefectRate).HasColumnName("transactionDefectRate").HasColumnType("decimal(9,4)");
        builder.Property(p => p.LateShipmentRate).HasColumnName("lateShipmentRate").HasColumnType("decimal(9,4)");
        builder.Property(p => p.TrackingUploadedOnTimeRate).HasColumnName("trackingUploadedOnTimeRate").HasColumnType("decimal(9,4)");
        builder.Property(p => p.CasesClosedWithoutSellerResolutionRate).HasColumnName("casesClosedWithoutSellerResolutionRate").HasColumnType("decimal(9,4)");
        builder.Property(p => p.TransactionsLast12Months).HasColumnName("transactionsLast12Months");
        builder.Property(p => p.SalesLast12Months).HasColumnName("salesLast12Months").HasColumnType("decimal(18,2)");
        builder.Property(p => p.LastEvaluatedAtUtc).HasColumnName("lastEvaluatedAtUtc");
        builder.Property(p => p.NextEvaluationDateUtc).HasColumnName("nextEvaluationDateUtc");
        builder.Property(p => p.ManualOverrideAtUtc).HasColumnName("manualOverrideAtUtc");
        builder.Property(p => p.ManualOverrideByAdminId).HasColumnName("manualOverrideByAdminId");

        builder.HasIndex(p => p.SellerId).IsUnique();
        builder.HasOne(p => p.Seller)
            .WithOne(u => u.Performance)
            .HasForeignKey<SellerPerformance>(p => p.SellerId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(p => p.ManualOverrideByAdminId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
