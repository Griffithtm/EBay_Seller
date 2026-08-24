using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class ReturnRequestConfiguration : IEntityTypeConfiguration<ReturnRequest>
{
    public void Configure(EntityTypeBuilder<ReturnRequest> builder)
    {
        builder.ToTable("ReturnRequest");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.OrderId).HasColumnName("orderId");
        builder.Property(r => r.UserId).HasColumnName("userId");
        builder.Property(r => r.Reason).HasColumnName("reason").HasMaxLength(2000).IsRequired();
        builder.Property(r => r.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("createdAt");

        builder.HasOne(r => r.Order)
            .WithMany()
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
            .WithMany(u => u.ReturnRequests)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.OrderId);
        builder.HasIndex(r => r.Status);
    }
}
