using LibrarySystem.Business.DTOs.Books;
using LibrarySystem.Shared.Results;

namespace LibrarySystem.Business.Interfaces;

/// <summary>
/// Book catalog operations exposed to both User and Admin roles.
/// </summary>
public interface IBookService
{
    /// <summary>
    /// Returns one page of books applying search, availability filtering,
    /// sorting and database-level pagination.
    /// </summary>
    /// <param name="query">Paging/filter/sort parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paged result of books or a validation failure.</returns>
    Task<Result<PagedResult<BookDto>>> GetBooksAsync(BookListQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single book by identifier.
    /// </summary>
    /// <param name="id">Book identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The book or a not-found failure.</returns>
    Task<Result<BookDto>> GetBookByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
