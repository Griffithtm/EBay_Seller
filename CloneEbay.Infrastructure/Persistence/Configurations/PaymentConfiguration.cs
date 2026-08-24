using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payment");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).HasColumnName("id");
        builder.Property(payment => payment.OrderId).HasColumnName("orderId");
        builder.Property(payment => payment.UserId).HasColumnName("userId");
        builder.Property(payment => payment.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(payment => payment.Method).HasColumnName("method").HasMaxLength(50);
        builder.Property(payment => payment.Status).HasColumnName("status").HasMaxLength(20);
        builder.Property(payment => payment.PaidAtUtc).HasColumnName("paidAt");

        builder.HasOne(payment => payment.Order)
            .WithOne(order => order.Payment)
            .HasForeignKey<Payment>(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(payment => payment.User)
            .WithMany()
            .HasForeignKey(payment => payment.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
