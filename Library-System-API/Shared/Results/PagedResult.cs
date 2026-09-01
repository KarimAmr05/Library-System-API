namespace LibrarySystem.Shared.Results;

/// <summary>
/// Standard paginated response envelope returned by all list endpoints:
/// { items, page, pageSize, totalItems, totalPages }.
/// </summary>
/// <typeparam name="T">Type of the items in the page.</typeparam>
public sealed record PagedResult<T>
{
    /// <summary>
    /// Gets the page of items.
    /// </summary>
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();

    /// <summary>
    /// Gets the 1-based page index this result represents.
    /// </summary>
    public int Page { get; init; }

    /// <summary>
    /// Gets the number of records requested per page.
    /// </summary>
    public int PageSize { get; init; }

    /// <summary>
    /// Gets the total number of items matching the query across all pages.
    /// </summary>
    public long TotalItems { get; init; }

    /// <summary>
    /// Gets the total number of pages available.
    /// </summary>
    public int TotalPages { get; init; }

    /// <summary>
    /// Creates a paginated result from an already-paged item list.
    /// </summary>
    /// <param name="items">Items belonging to the requested page.</param>
    /// <param name="page">1-based page index.</param>
    /// <param name="pageSize">Page size used to build the page.</param>
    /// <param name="totalItems">Total matching items across all pages.</param>
    /// <returns>The populated <see cref="PagedResult{T}"/>.</returns>
    public static PagedResult<T> Create(IReadOnlyList<T> items, int page, int pageSize, long totalItems) =>
        new()
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalItems = totalItems,
            TotalPages = pageSize > 0 ? (int)Math.Ceiling(totalItems / (double)pageSize) : 0
        };
}
