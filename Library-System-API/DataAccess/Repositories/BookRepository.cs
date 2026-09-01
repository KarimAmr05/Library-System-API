using LibrarySystem.DataAccess.Entities;
using LibrarySystem.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.DataAccess.Repositories;

/// <summary>
/// Book repository with database-level paging, filtering and sorting.
/// </summary>
/// <param name="context">The database context.</param>
public class BookRepository(DbContext context)
    : GenericRepository<Book>(context), IBookRepository
{
    private static readonly Dictionary<string, string> SortMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["title"] = nameof(Book.Title),
            ["author"] = nameof(Book.Author),
            ["createdat"] = nameof(Book.CreatedAt)
        };

    /// <inheritdoc />
    public async Task<(IReadOnlyList<Book> Items, long TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? availableOnly,
        string? sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default)
    {
        var query = Query();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(b =>
                EF.Functions.Like(b.Title, $"%{term}%") ||
                EF.Functions.Like(b.Author, $"%{term}%") ||
                (b.Category != null && EF.Functions.Like(b.Category, $"%{term}%")));
        }

        if (availableOnly == true)
        {
            query = query.Where(b => b.IsAvailable && b.AvailableCopies > 0);
        }

        long totalItems = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        var sortField = sortBy is not null && SortMap.TryGetValue(sortBy.Trim(), out var mapped)
            ? mapped
            : nameof(Book.Title);

        var ordered = sortDescending
            ? query.OrderByDescending(b => EF.Property<object>(b, sortField))
            : query.OrderBy(b => EF.Property<object>(b, sortField));

        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return (items, totalItems);
    }
}
