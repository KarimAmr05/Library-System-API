using LibrarySystem.API.Extensions;
using LibrarySystem.Business.DTOs.Notifications;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Shared.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.API.Controllers;

/// <summary>
/// Notification inbox endpoints. Recipient scoping is always enforced from JWT
/// claims so users can never read or modify another user's notifications.
/// </summary>
[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    private readonly INotificationService _notificationService = notificationService;

    /// <summary>
    /// Lists the caller's notifications with paging and read-state filters.
    /// Admins may additionally filter by recipient/role.
    /// </summary>
    /// <param name="query">Paging/filter parameters.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A paged list of notifications.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetNotifications([FromQuery] NotificationsQueryDto query,
        CancellationToken cancellationToken)
    {
        var result = await _notificationService.GetNotificationsAsync(
            query, User.GetUserId(), User.GetRole(), cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }

    /// <summary>
    /// Marks one of the caller's notifications as read.
    /// </summary>
    /// <param name="id">Notification identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The updated notification.</returns>
    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var result = await _notificationService.MarkAsReadAsync(id, User.GetUserId(), User.GetRole(),
            cancellationToken).ConfigureAwait(false);

        return result.IsSuccess
            ? Ok(result.Value)
            : result.ToProblemResult(HttpContext.TraceIdentifier);
    }
}
