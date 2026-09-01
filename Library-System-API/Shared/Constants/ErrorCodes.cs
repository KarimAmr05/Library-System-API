namespace LibrarySystem.Shared.Constants;

/// <summary>
/// Central registry of stable machine-readable error codes returned in error payloads.
/// These codes form part of the API contract and must not be renamed.
/// </summary>
public static class ErrorCodes
{
    /// <summary>The request payload failed validation.</summary>
    public const string ValidationError = "VALIDATION_ERROR";

    /// <summary>Authentication is required or the supplied token is invalid.</summary>
    public const string Unauthorized = "UNAUTHORIZED";

    /// <summary>The authenticated identity lacks permission for the operation.</summary>
    public const string Forbidden = "FORBIDDEN";

    /// <summary>The requested resource does not exist.</summary>
    public const string NotFound = "NOT_FOUND";

    /// <summary>The operation conflicts with current business state.</summary>
    public const string Conflict = "CONFLICT";

    /// <summary>A business rule prevented the operation from completing.</summary>
    public const string BusinessRuleViolation = "BUSINESS_RULE_VIOLATION";

    /// <summary>An unexpected backend failure occurred.</summary>
    public const string InternalError = "INTERNAL_ERROR";
}
