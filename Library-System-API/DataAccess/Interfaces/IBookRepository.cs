using LibrarySystem.DataAccess.Entities;

namespace LibrarySystem.DataAccess.Interfaces;

/// <summary>
/// Repository contract with book-specific read operations.
/// </summary>
public interface IBookRepository : IGenericRepository<Book>
{
    /// <summary>
    /// Returns one page of books filtered/sorted at the database level.
    /// </summary>
    /// <param name="page">1-based page index.</param>
    /// <param name="pageSize">Records per page (max 100 enforced upstream).</param>
    /// <param name="search">Free-text filter matched against title/author/category.</param>
    /// <param name="availableOnly">When true, restricts to books with available copies.</param>
    /// <param name="sortBy">Sort field: title, author or createdAt.</param>
    /// <param name="sortDescending">Whether sorting is descending.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The paged items together with the total matching count.</returns>
    Task<(IReadOnlyList<Book> Items, long TotalItems)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? availableOnly,
        string? sortBy,
        bool sortDescending,
        CancellationToken cancellationToken = default);
}
