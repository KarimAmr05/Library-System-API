namespace LibrarySystem.Shared.Enums;

/// <summary>
/// Defines the types of notifications the system can produce.
/// </summary>
public enum NotificationType
{
    /// <summary>Notifies admins that a new borrowing request was created.</summary>
    BorrowRequestCreated = 0,

    /// <summary>Reminds a user that their borrowing period is nearing expiration.</summary>
    BorrowDueReminder = 1,

    /// <summary>Notifies a user that their borrowing request was approved.</summary>
    RequestApproved = 2,

    /// <summary>Notifies a user that their borrowing request was denied.</summary>
    RequestDenied = 3
}
