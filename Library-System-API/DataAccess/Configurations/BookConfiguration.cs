using LibrarySystem.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.DataAccess.Configurations;

/// <summary>
/// EF Core configuration for the <see cref="Book"/> entity.
/// </summary>
public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    /// <summary>Configures the <see cref="Book"/> entity mapping.</summary>
    /// <param name="builder">The builder used to construct the entity type.</param>
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.Isbn)
            .HasMaxLength(20);

        builder.Property(b => b.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(b => b.Author)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Category)
            .HasMaxLength(100);

        builder.Property(b => b.TotalCopies)
            .IsRequired();

        builder.Property(b => b.AvailableCopies)
            .IsRequired();

        // Guard against invalid negative availability at the database level.
        builder.ToTable(t => t.HasCheckConstraint("CK_Books_AvailableCopies_NonNegative",
            "[AvailableCopies] >= 0"));
        builder.ToTable(t => t.HasCheckConstraint("CK_Books_AvailableCopies_MaxTotal",
            "[AvailableCopies] <= [TotalCopies]"));

        builder.HasIndex(b => b.Title);
        builder.HasIndex(b => b.Author);
        builder.HasIndex(b => b.IsAvailable);

        builder.Property(b => b.CreatedAt).IsRequired();
        builder.Property(b => b.UpdatedAt).IsRequired();
    }
}
