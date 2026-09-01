using LibrarySystem.API.Extensions;
using LibrarySystem.Business.DTOs.Requests;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.API.Controllers;

/// <summary>
/// Borrowing-request endpoints: submission via RabbitMQ-backed processing,
/// admin listing for review, user history and single-request retrieval.
/// </summary>
[ApiController]
[Route("api")]
[Authorize]
public class BorrowingRequestsController(IBorrowingService borrowingService) : ControllerBase
{
    private readonly IBorrowingService _borrowingService = borrowingService;

    /// <summary>
    /// Submits a new borrowing request. The pending request is persisted and handed
    /// off to RabbitMQ for asynchronous processing (admin notifications via SignalR).
    /// </summary>
    /// <param name="request">Request payload; userId is validated against the JWT identity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>201 with the created request; 400/401/403/409/422 on failures.</returns>
    [HttpPost("borrow")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(typeof(BorrowingRequestDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] BorrowRequestCreateDto request,
        CancellationToken cancellationToken)
    {
        var result = await _borrowingService.CreateRequestAsync(
            request,
            User.GetUserId(),
            User.GetRole(),
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? CreatedAtAction(nameof(GetRequest), new { id = result.Value.Id }, result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Lists all borrowing requests for admin review (Admin only).
    /// </summary>
    /// <param name="query">Paging/filter parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paged list of requests.</returns>
    [HttpGet("requests")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(PagedResult<BorrowingRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetRequests([FromQuery] RequestsListQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _borrowingService.GetRequestsAsync(query, cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Lists the authenticated user's own borrowing history.
    /// </summary>
    /// <param name="query">Paging/filter parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paged list of the user's requests.</returns>
    [HttpGet("requests/my")]
    [Authorize(Roles = "User")]
    [ProducesResponseType(typeof(PagedResult<BorrowingRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMyRequests([FromQuery] RequestsListQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _borrowingService.GetMyRequestsAsync(query, User.GetUserId(), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Gets a single borrowing request. Regular users may only view their own requests.
    /// </summary>
    /// <param name="id">Request identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The requested borrowing request.</returns>
    [HttpGet("requests/{id:guid}")]
    [ProducesResponseType(typeof(BorrowingRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequest(Guid id, CancellationToken cancellationToken)
    {
        var result = await _borrowingService.GetRequestByIdAsync(id, User.GetUserId(), User.GetRole(),
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }
}
