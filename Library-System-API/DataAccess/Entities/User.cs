using LibrarySystem.Shared.Enums;

namespace LibrarySystem.DataAccess.Entities;

/// <summary>
/// Represents a registered user of the library system.
/// </summary>
public class User
{
    /// <summary>Gets or sets the unique identifier of the user.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the display name of the user.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the email address used for sign-in. Unique across users.</summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>Hash of the user's password. Never exposed through API contracts.</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>Gets or sets the role assigned to the user.</summary>
    public UserRole Role { get; set; }

    /// <summary>Gets or sets a value indicating whether the account may access the system.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets or sets the UTC timestamp at which the account was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets the collection of borrowing requests submitted by this user.</summary>
    public ICollection<BorrowingRequest> BorrowingRequests { get; set; } = new List<BorrowingRequest>();
}
