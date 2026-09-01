using LibrarySystem.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.DataAccess.Context;

/// <summary>
/// Entity Framework Core database context for the library system.
/// All persistence is coordinated through repositories and the Unit of Work;
/// business and API layers never interact with this class directly.
/// </summary>
/// <param name="options">Context options configured at startup.</param>
public class LibraryDBContext(DbContextOptions<LibraryDBContext> options) : DbContext(options)
{
    /// <summary>
    /// Gets or sets the registered users.
    /// </summary>
    public DbSet<User> Users { get; set; }

    /// <summary>
    /// Gets or sets the library book catalog.
    /// </summary>
    public DbSet<Book> Books { get; set; }

    /// <summary>
    /// Gets or sets the borrowing requests.
    /// </summary>
    public DbSet<BorrowingRequest> BorrowingRequests { get; set; }

    /// <summary>
    /// Gets or sets the persisted notifications.
    /// </summary>
    public DbSet<Notification> Notifications { get; set; }

    /// <summary>
    /// Gets or sets the one-time password reset tokens.
    /// </summary>
    public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
    /// <summary>
    /// Configures the entity mappings using explicit <c>IEntityTypeConfiguration</c> classes.
    /// </summary>
    /// <param name="modelBuilder">The model builder.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LibraryDBContext).Assembly);
    }
}
