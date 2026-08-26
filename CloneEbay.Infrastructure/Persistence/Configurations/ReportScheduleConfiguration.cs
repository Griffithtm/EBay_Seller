using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class ReportScheduleConfiguration : IEntityTypeConfiguration<ReportSchedule>
{
    public void Configure(EntityTypeBuilder<ReportSchedule> builder)
    {
        builder.ToTable("ReportSchedule");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.UserId).HasColumnName("userId");
        builder.Property(s => s.Source).HasColumnName("source").HasMaxLength(64).IsRequired();
        builder.Property(s => s.Type).HasColumnName("type").HasMaxLength(128).IsRequired();
        builder.Property(s => s.Frequency).HasColumnName("frequency").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(s => s.CreatedAtUtc).HasColumnName("createdAtUtc");
        builder.Property(s => s.LastRunAtUtc).HasColumnName("lastRunAtUtc");
        builder.Property(s => s.NextRunAtUtc).HasColumnName("nextRunAtUtc");
        builder.Property(s => s.EndDateUtc).HasColumnName("endDateUtc");
        builder.Property(s => s.IsActive).HasColumnName("isActive");
        builder.Property(s => s.DeliveryEmail).HasColumnName("deliveryEmail").HasMaxLength(256);

        builder.HasIndex(s => new { s.UserId, s.Source, s.Type, s.IsActive });
        builder.HasIndex(s => new { s.IsActive, s.NextRunAtUtc });

        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
