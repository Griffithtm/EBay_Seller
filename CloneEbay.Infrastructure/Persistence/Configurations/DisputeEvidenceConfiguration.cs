using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class DisputeEvidenceConfiguration : IEntityTypeConfiguration<DisputeEvidence>
{
    public void Configure(EntityTypeBuilder<DisputeEvidence> builder)
    {
        builder.ToTable("DisputeEvidence");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");
        builder.Property(e => e.DisputeId).HasColumnName("disputeId");
        builder.Property(e => e.UploaderId).HasColumnName("uploaderId");
        builder.Property(e => e.FilePath).HasColumnName("filePath").HasMaxLength(1000).IsRequired();
        builder.Property(e => e.FileName).HasColumnName("fileName").HasMaxLength(255).IsRequired();
        builder.Property(e => e.ContentType).HasColumnName("contentType").HasMaxLength(100);
        builder.Property(e => e.CreatedAt).HasColumnName("createdAt");

        builder.HasOne(e => e.Uploader)
            .WithMany()
            .HasForeignKey(e => e.UploaderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.DisputeId);
    }
}
