using LibrarySystem.Business.DTOs.Books;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Business.Mappings;
using LibrarySystem.DataAccess.UnitOfWork;
using LibrarySystem.Shared.Enums;
using LibrarySystem.Shared.Results;

namespace LibrarySystem.Business.Services;

/// <summary>
/// Book catalog service. All reads execute at the database level with
/// no-tracking semantics.
/// </summary>
/// <param name="unitOfWork">Unit of work for data access.</param>
public sealed class BookService(IUnitOfWork unitOfWork) : IBookService
{
    private static readonly HashSet<string> ValidSortFields =
        new(StringComparer.OrdinalIgnoreCase) { "title", "author", "createdat" };

    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    /// <inheritdoc />
    public async Task<Result<PagedResult<BookDto>>> GetBooksAsync(
        BookListQueryDto query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var validation = Validators.DtoValidator.Validate(query);
        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<BookDto>>([.. validation.Errors]);
        }

        // Unknown sort fields fall back to title rather than failing the request.
        var sortBy = query.SortBy is null || ValidSortFields.Contains(query.SortBy.Trim())
            ? query.SortBy
            : null;

        var result = await _unitOfWork.Books.GetPagedAsync(
            query.Page,
            query.PageSize,
            query.Search,
            query.AvailableOnly,
            sortBy,
            IsDescending(query.SortOrder),
            cancellationToken).ConfigureAwait(false);

        return Result.Success(result.ToDto(query.Page, query.PageSize, b => b.ToDto()));
    }

    /// <inheritdoc />
    public async Task<Result<BookDto>> GetBookByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var book = await _unitOfWork.Books.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return book is null
            ? Result.Failure<BookDto>(Error.NotFound("Book"))
            : Result.Success(book.ToDto());
    }

    private static bool IsDescending(string sortOrder) =>
        string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);
}
