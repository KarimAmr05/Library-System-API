namespace LibrarySystem.Business.Services;

/// <summary>
/// Internal outcome of a borrowing-request state transition executed inside
/// a transaction. Exposed as internal for testability.
/// </summary>
internal enum TransitionOutcome
{
    /// <summary>The request row does not exist.</summary>
    NotFound,

    /// <summary>The request is not in the Pending state.</summary>
    InvalidState,

    /// <summary>The related book has no available copies.</summary>
    NoCopiesAvailable,

    /// <summary>The transition was applied and committed.</summary>
    Applied
}
