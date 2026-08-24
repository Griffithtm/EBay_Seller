using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class DisputeConfiguration : IEntityTypeConfiguration<Dispute>
{
    public void Configure(EntityTypeBuilder<Dispute> builder)
    {
        builder.ToTable("Dispute");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.OrderId).HasColumnName("orderId");
        builder.Property(d => d.RaisedBy).HasColumnName("raisedBy");
        builder.Property(d => d.Description).HasColumnName("description").HasMaxLength(2000).IsRequired();
        builder.Property(d => d.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(d => d.Resolution).HasColumnName("resolution").HasMaxLength(2000);

        builder.HasOne(d => d.Order)
            .WithMany()
            .HasForeignKey(d => d.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Raiser)
            .WithMany(u => u.Disputes)
            .HasForeignKey(d => d.RaisedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.Status);
        builder.HasIndex(d => d.OrderId);
    }
}
