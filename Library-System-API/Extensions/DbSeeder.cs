using LibrarySystem.DataAccess.Context;
using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.Seeding;
using LibrarySystem.Shared.Enums;
using LibrarySystem.Shared.Security;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.API.Extensions;

/// <summary>
/// Seeds baseline data so the system can be exercised immediately in
/// development. Credentials below are development-only placeholders and must be
/// changed or removed for production deployments.
/// Seeding is idempotent: users are inserted only when the Users table is empty
/// and books only when the Books table is empty, so application restarts never
/// create duplicate records.
/// </summary>
public static class DbSeeder
{
    /// <summary>
    /// Runs pending migrations and seeds default accounts plus a realistic
    /// book catalog. Runs only outside production.
    /// </summary>
    /// <param name="services">Root service provider.</param>
    /// <param name="environment">Hosting environment.</param>
    public static async Task SeedDevelopmentDataAsync(this IServiceProvider services,
        IWebHostEnvironment environment)
    {
        if (environment.IsEnvironment("Production"))
        {
            return;
        }

        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDBContext>();

        await context.Database.MigrateAsync().ConfigureAwait(false);

        await SeedUsersAsync(context).ConfigureAwait(false);
        await SeedBooksAsync(context).ConfigureAwait(false);
    }

    private static async Task SeedUsersAsync(LibraryDBContext context)
    {
        if (await context.Users.AnyAsync().ConfigureAwait(false))
        {
            return;
        }

        context.Users.AddRange(
            new User
            {
                Id = Guid.NewGuid(),
                FullName = "Library Admin",
                Email = "admin@library.local",
                PasswordHash = PasswordHasher.Hash("Admin@12345"),
                Role = UserRole.Admin,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = Guid.NewGuid(),
                FullName = "Demo User",
                Email = "user@library.local",
                PasswordHash = PasswordHasher.Hash("User@12345"),
                Role = UserRole.User,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private static async Task SeedBooksAsync(LibraryDBContext context)
    {
        if (await context.Books.AnyAsync().ConfigureAwait(false))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var seedBooks = LibraryBookSeedData.Books;

        var entities = new List<Book>(seedBooks.Count);
        for (var i = 0; i < seedBooks.Count; i++)
        {
            var seed = seedBooks[i];

            // Acquisitions spread gradually over ~2 years of library history.
            var createdAt = now
                .AddDays(-(seedBooks.Count - i) * 11 - (i % 5) * 2)
                .AddHours(i % 9);

            // Books whose copies were borrowed/returned have a later update time.
            // Invariant: updatedAt >= createdAt and never in the future.
            var updatedAt = seed.AvailableCopies == seed.TotalCopies
                ? createdAt
                : createdAt.AddDays(21 + (i % 45) * 6);

            if (updatedAt > now)
            {
                updatedAt = now.AddHours(-i);
            }

            entities.Add(new Book
            {
                Id = Guid.NewGuid(),
                Isbn = seed.Isbn,
                Title = seed.Title,
                Author = seed.Author,
                Category = seed.Category,
                IsAvailable = seed.AvailableCopies > 0,
                TotalCopies = seed.TotalCopies,
                AvailableCopies = seed.AvailableCopies,
                CreatedAt = createdAt,
                UpdatedAt = updatedAt
            });
        }

        context.Books.AddRange(entities);
        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
