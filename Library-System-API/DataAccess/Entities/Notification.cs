using LibrarySystem.Shared.Enums;

namespace LibrarySystem.DataAccess.Entities;

/// <summary>
/// Represents a persisted notification delivered to a user or admin inbox.
/// </summary>
public class Notification
{
    /// <summary>Gets or sets the unique identifier of the notification.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the identifier of the recipient user.</summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>Gets or sets the role of the recipient used for admin-wide feeds.</summary>
    public UserRole RecipientRole { get; set; }

    /// <summary>Gets or sets the type of the notification.</summary>
    public NotificationType Type { get; set; }

    /// <summary>Gets or sets the short title shown in inboxes.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the detailed message body.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the recipient has read the notification.</summary>
    public bool IsRead { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which the notification was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the optional related borrowing request.
    /// Internal correlation field used by background jobs to prevent duplicate reminders;
    /// never exposed in API contracts.
    /// </summary>
    public Guid? RelatedRequestId { get; set; }
}
