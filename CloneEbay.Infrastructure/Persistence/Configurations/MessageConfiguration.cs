using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Content)
            .IsRequired()
            .HasMaxLength(2000);
            
        builder.Property(m => m.Timestamp)
            .IsRequired();
            
        builder.Property(m => m.IsRead)
            .IsRequired()
            .HasDefaultValue(false);
            
        builder.HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(m => m.Receiver)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.ReceiverId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasIndex(m => new { m.ReceiverId, m.IsRead });
        builder.HasIndex(m => new { m.SenderId, m.ReceiverId, m.Timestamp });
    }
}
