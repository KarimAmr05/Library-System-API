using LibrarySystem.API.Extensions;
using LibrarySystem.Business.DTOs.Books;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.API.Controllers;

/// <summary>
/// Book catalog endpoints available to all authenticated roles.
/// </summary>
[ApiController]
[Route("api/books")]
[Authorize]
public class BooksController(IBookService bookService) : ControllerBase
{
    private readonly IBookService _bookService = bookService;

    /// <summary>
    /// Lists books with paging, free-text search, availability filtering and sorting.
    /// </summary>
    /// <param name="query">Paging/filter/sort parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paged list of books.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<BookDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    
    public async Task<IActionResult> GetBooks([FromQuery] BookListQueryDto query, CancellationToken cancellationToken)
    {
        var result = await _bookService.GetBooksAsync(query, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Gets a single book by identifier.
    /// </summary>
    /// <param name="id">Book identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The requested book.</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
  
    public async Task<IActionResult> GetBook(Guid id, CancellationToken cancellationToken)
    {
        var result = await _bookService.GetBookByIdAsync(id, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }
}
