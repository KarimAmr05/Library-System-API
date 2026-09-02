using LibrarySystem.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.DataAccess.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="RefreshToken"/> entity.
/// </summary>
public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    /// <summary>Configures the <see cref="RefreshToken"/> entity mapping.</summary>
    /// <param name="builder">The builder used to construct the entity type.</param>
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        // Token validation is always by hash.
        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        // Sweep candidates for cleanup jobs and per-user revocation.
        builder.HasIndex(t => new { t.UserId, t.RevokedAtUtc });

        builder.Property(t => t.ExpiresAtUtc)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        // When a user is deleted their refresh tokens go with them.
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}