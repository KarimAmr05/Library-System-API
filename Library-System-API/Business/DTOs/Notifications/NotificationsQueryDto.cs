using System.ComponentModel.DataAnnotations;
using LibrarySystem.Shared.Enums;

namespace LibrarySystem.Business.DTOs.Notifications;

/// <summary>
/// Query parameters for GET /api/notifications.
/// The recipient scope is enforced from JWT claims; query filters may only narrow it.
/// </summary>
public class NotificationsQueryDto
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    /// <summary>Gets or sets the 1-based page index (minimum 1).</summary>
    [Range(1, int.MaxValue)]
    public int Page { get => _page; set => _page = value < 1 ? 1 : value; }

    /// <summary>Gets or sets the page size. Defaults to 20 and is capped at 100.</summary>
    [Range(1, MaxPageSize)]
    public int PageSize { get => _pageSize; set => _pageSize = value is < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize); }

    /// <summary>Gets or sets the optional recipient filter. Non-admin callers are always overridden with their own id.</summary>
    public Guid? RecipientUserId { get; init; }

    /// <summary>Gets or sets the optional role filter.</summary>
    public UserRole? RecipientRole { get; init; }

    /// <summary>Gets or sets the optional read-state filter.</summary>
    public bool? IsRead { get; init; }
}
