using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class ReportDownloadConfiguration : IEntityTypeConfiguration<ReportDownload>
{
    public void Configure(EntityTypeBuilder<ReportDownload> builder)
    {
        builder.ToTable("ReportDownload");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id");
        builder.Property(d => d.UserId).HasColumnName("userId");
        builder.Property(d => d.ReferenceCode).HasColumnName("referenceCode").HasMaxLength(32).IsRequired();
        builder.Property(d => d.Source).HasColumnName("source").HasMaxLength(64).IsRequired();
        builder.Property(d => d.Type).HasColumnName("type").HasMaxLength(128).IsRequired();
        builder.Property(d => d.RequestedAtUtc).HasColumnName("requestedAtUtc");
        builder.Property(d => d.CompletedAtUtc).HasColumnName("completedAtUtc");
        builder.Property(d => d.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(d => d.FileUrl).HasColumnName("fileUrl").HasMaxLength(512);
        builder.Property(d => d.RangeStartUtc).HasColumnName("rangeStartUtc");
        builder.Property(d => d.RangeEndUtc).HasColumnName("rangeEndUtc");
        builder.Property(d => d.RangeTimeZone).HasColumnName("rangeTimeZone").HasMaxLength(64);

        // spec A.2: unique per (UserId, ReferenceCode).
        builder.HasIndex(d => new { d.UserId, d.ReferenceCode }).IsUnique();
        builder.HasIndex(d => new { d.UserId, d.RequestedAtUtc });

        builder.HasOne(d => d.User)
            .WithMany()
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
