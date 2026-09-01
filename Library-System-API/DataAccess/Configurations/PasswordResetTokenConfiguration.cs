using LibrarySystem.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.DataAccess.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="PasswordResetToken"/> entity.
/// </summary>
public sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    /// <summary>Configures the <see cref="PasswordResetToken"/> entity mapping.</summary>
    /// <param name="builder">The builder used to construct the entity type.</param>
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("PasswordResetTokens");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .ValueGeneratedNever();

        builder.Property(t => t.UserId)
            .IsRequired();

        builder.Property(t => t.TokenHash)
            .IsRequired()
            .HasMaxLength(64);

        // Token lookups are always by hash.
        builder.HasIndex(t => t.TokenHash)
            .IsUnique();

        // Sweep candidates for cleanup jobs.
        builder.HasIndex(t => new { t.UserId, t.UsedAtUtc });

        builder.Property(t => t.ExpiresAtUtc)
            .IsRequired();

        builder.Property(t => t.CreatedAtUtc)
            .IsRequired();

        // When a user is deleted their reset tokens go with them.
        builder.HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
