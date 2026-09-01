namespace LibrarySystem.Shared.Enums;

/// <summary>
/// Represents the lifecycle status of a borrowing request.
/// Lifecycle: Pending → Approved → Returned | Expired, or Pending → Denied.
/// </summary>
public enum BorrowingRequestStatus
{
    /// <summary>The request has been submitted and awaits an admin decision.</summary>
    Pending = 0,

    /// <summary>The request was approved by an administrator.</summary>
    Approved = 1,

    /// <summary>The request was denied by an administrator.</summary>
    Denied = 2,

    /// <summary>The borrowed book was returned before the due date.</summary>
    Returned = 3,

    /// <summary>The borrowing period elapsed without a return.</summary>
    Expired = 4
}
