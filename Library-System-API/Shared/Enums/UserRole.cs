namespace LibrarySystem.Shared.Enums;

/// <summary>
/// Defines the roles a user of the library system can hold.
/// </summary>
public enum UserRole
{
    /// <summary>A regular library user who can browse and borrow books.</summary>
    User = 0,

    /// <summary>An administrator who reviews and manages borrowing requests.</summary>
    Admin = 1
}
