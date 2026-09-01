using LibrarySystem.DataAccess.Entities;
using LibrarySystem.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.DataAccess.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="BorrowingRequest"/> entity.
/// </summary>
public sealed class BorrowingRequestConfiguration : IEntityTypeConfiguration<BorrowingRequest>
{
    private const int MaxPeriodDays = 30;

    /// <summary>Configures the <see cref="BorrowingRequest"/> entity mapping.</summary>
    /// <param name="builder">The builder used to construct the entity type.</param>
    public void Configure(EntityTypeBuilder<BorrowingRequest> builder)
    {
        builder.ToTable("BorrowingRequests");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.BookTitle)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(r => r.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.BorrowingPeriodDays)
            .IsRequired();

        builder.Property(r => r.DenyReason)
            .HasMaxLength(1000);

        builder.HasOne(r => r.Book)
            .WithMany()
            .HasForeignKey(r => r.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.User)
            .WithMany(u => u.BorrowingRequests)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.Status);
        builder.HasIndex(r => r.UserId);
        builder.HasIndex(r => r.BookId);
        builder.HasIndex(r => r.RequestedAt);

        builder.ToTable(t => t.HasCheckConstraint("CK_BorrowingRequests_PeriodRange",
            $"[BorrowingPeriodDays] >= 1 AND [BorrowingPeriodDays] <= {MaxPeriodDays}"));

        // Deny reason is only meaningful for denied requests.
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_BorrowingRequests_DenyReason",
            "([Status] = 'Denied' AND [DenyReason] IS NOT NULL) OR ([Status] <> 'Denied' AND [DenyReason] IS NULL)"));
    }
}
