using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("User");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).HasColumnName("id");
        builder.Property(user => user.Username).HasColumnName("username").HasMaxLength(100).IsRequired();
        builder.Property(user => user.Email).HasColumnName("email").HasMaxLength(100).IsRequired();
        builder.Property(user => user.PasswordHash).HasColumnName("password").HasMaxLength(512).IsRequired();
        builder.Property(user => user.Role).HasColumnName("role").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(user => user.AvatarUrl).HasColumnName("avatarURL").HasMaxLength(2048);
        builder.Property(user => user.EmailConfirmed).HasColumnName("emailConfirmed").HasDefaultValue(false);
        builder.Property(user => user.IsActive).HasColumnName("isActive").HasDefaultValue(true);
        builder.Property(user => user.FailedLoginAttempts).HasColumnName("failedLoginAttempts").HasDefaultValue(0);
        builder.Property(user => user.LockoutEndUtc).HasColumnName("lockoutEndUtc");
        builder.Property(user => user.CreatedAtUtc).HasColumnName("createdAtUtc");
        builder.Property(user => user.UpdatedAtUtc).HasColumnName("updatedAtUtc");

        builder.HasIndex(user => user.Email).IsUnique();
        builder.HasIndex(user => user.Username);
    }
}
