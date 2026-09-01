namespace LibrarySystem.Business.DTOs.Notifications;

/// <summary>
/// Notification resource exposed by the API.
/// Mirrors the documented Notification model.
/// </summary>
public class NotificationDto
{
    /// <summary>Gets the unique notification identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets the recipient user identifier.</summary>
    public Guid RecipientUserId { get; init; }

    /// <summary>Gets the recipient role: User or Admin.</summary>
    public string RecipientRole { get; init; } = string.Empty;

    /// <summary>Gets the notification type.</summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>Gets the short title.</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Gets the detailed message body.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Gets a value indicating whether the notification has been read.</summary>
    public bool IsRead { get; init; }

    /// <summary>Gets the creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; init; }
}
