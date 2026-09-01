using LibrarySystem.API.Extensions;
using LibrarySystem.Business.DTOs.Requests;
using LibrarySystem.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.API.Controllers;

/// <summary>
/// Admin-only decision endpoints. Every decision records the reviewing admin
/// (actor) and timestamp for audit purposes.
/// </summary>
[ApiController]
[Route("api/requests")]
[Authorize(Roles = "Admin")]
public class AdminActionsController(IBorrowingService borrowingService) : ControllerBase
{
    private readonly IBorrowingService _borrowingService = borrowingService;

    /// <summary>
    /// Approves a pending request. Decrements book availability atomically.
    /// </summary>
    /// <param name="id">Request identifier.</param>
    /// <param name="request">Approval payload; approvedByAdminId must match the token identity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The approved request.</returns>
    [HttpPut("{id:guid}/approve")]
    [ProducesResponseType(typeof(BorrowingRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Approve(Guid id, [FromBody] BorrowRequestApproveDto request,
        CancellationToken cancellationToken)
    {
        var result = await _borrowingService.ApproveAsync(id, request, User.GetUserId(), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Denies a pending request with a required reason.
    /// </summary>
    /// <param name="id">Request identifier.</param>
    /// <param name="request">Denial payload; deniedByAdminId must match the token identity.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The denied request.</returns>
    [HttpPut("{id:guid}/deny")]
    [ProducesResponseType(typeof(BorrowingRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Deny(Guid id, [FromBody] BorrowRequestDenyDto request,
        CancellationToken cancellationToken)
    {
        var result = await _borrowingService.DenyAsync(id, request, User.GetUserId(), cancellationToken)
            .ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }
}
