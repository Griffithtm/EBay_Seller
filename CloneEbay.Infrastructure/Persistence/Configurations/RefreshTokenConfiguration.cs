using CloneEbay.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloneEbay.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken");
        builder.HasKey(token => token.Id);
        builder.Property(token => token.Id).HasColumnName("id");
        builder.Property(token => token.UserId).HasColumnName("userId");
        builder.Property(token => token.TokenHash).HasColumnName("tokenHash").HasMaxLength(64).IsRequired();
        builder.Property(token => token.ExpiresAtUtc).HasColumnName("expiresAtUtc");
        builder.Property(token => token.CreatedAtUtc).HasColumnName("createdAtUtc");
        builder.Property(token => token.RevokedAtUtc).HasColumnName("revokedAtUtc");
        builder.Property(token => token.ReplacedByTokenHash).HasColumnName("replacedByTokenHash").HasMaxLength(64);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.UserId, token.ExpiresAtUtc });
        builder.HasOne(token => token.User)
            .WithMany(user => user.RefreshTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
