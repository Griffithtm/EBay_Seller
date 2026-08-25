using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class DisputeMessageConfiguration : IEntityTypeConfiguration<DisputeMessage>
{
    public void Configure(EntityTypeBuilder<DisputeMessage> builder)
    {
        builder.ToTable("DisputeMessage");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id");
        builder.Property(m => m.DisputeId).HasColumnName("disputeId");
        builder.Property(m => m.SenderId).HasColumnName("senderId");
        builder.Property(m => m.SenderRole).HasColumnName("senderRole").HasMaxLength(20);
        builder.Property(m => m.Message).HasColumnName("message").HasMaxLength(2000).IsRequired();
        builder.Property(m => m.CreatedAt).HasColumnName("createdAt");

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(m => m.DisputeId);
    }
}
