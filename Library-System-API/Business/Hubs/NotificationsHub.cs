using LibrarySystem.Shared.Constants;
using LibrarySystem.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace LibrarySystem.Business.Hubs;

/// <summary>
/// Real-time notification hub. Clients authenticate with a JWT (access_token
/// query string per SignalR negotiation) and are joined to role/user groups.
/// </summary>
[Authorize]
public class NotificationsHub : Hub
{
    /// <summary>Group receiving broadcasts targeted at all administrators.</summary>
    public const string AdminsGroup = "role-admins";

    /// <summary>Builds the per-user group name for targeted delivery.</summary>
    /// <param name="userId">User identifier.</param>
    /// <returns>The group name.</returns>
    public static string UserGroup(Guid userId) => $"user-{userId}";

    /// <summary>
    /// Joins the connecting client to its role and personal user groups.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (Context.User?.IsInRole(UserRole.Admin.ToString()) == true)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminsGroup).ConfigureAwait(false);
        }

        if (Guid.TryParse(userId, out var parsedUserId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(parsedUserId)).ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Removes the disconnecting client from its groups.
    /// </summary>
    /// <param name="exception">The exception that caused the disconnect, if any.</param>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                     ?? Context.User?.FindFirst("sub")?.Value;

        if (Context.User?.IsInRole(UserRole.Admin.ToString()) == true)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, AdminsGroup).ConfigureAwait(false);
        }

        if (Guid.TryParse(userId, out var parsedUserId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, UserGroup(parsedUserId)).ConfigureAwait(false);
        }

        await base.OnDisconnectedAsync(exception).ConfigureAwait(false);
    }
}
