using System.Text.Json.Serialization;

namespace LibrarySystem.Shared.Enums;

/// <summary>
/// Defines the roles a user of the library system can hold.
/// Serialized as its name string in JSON ("User"/"Admin") to match the
/// API's string-based conventions (DB storage and DTOs also use strings).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum UserRole
{
    /// <summary>A regular library user who can browse and borrow books.</summary>
    User = 0,

    /// <summary>An administrator who reviews and manages borrowing requests.</summary>
    Admin = 1
}