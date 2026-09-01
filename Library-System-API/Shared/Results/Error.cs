namespace LibrarySystem.Shared.Results;

/// <summary>
/// Describes a single business or system error with a machine-readable code,
/// a human-readable message and optional per-field validation details.
/// </summary>
/// <param name="Code">Stable machine-readable error code (see <see cref="Constants.ErrorCodes"/>).</param>
/// <param name="Message">Human-readable description of the error.</param>
/// <param name="Field">Optional name of the request field the error relates to.</param>
public sealed record Error(string Code, string Message, string? Field = null)
{
    /// <summary>
    /// Creates a field-level validation error.
    /// </summary>
    /// <param name="field">Name of the invalid field.</param>
    /// <param name="message">Description of why the field is invalid.</param>
    /// <returns>The constructed validation <see cref="Error"/>.</returns>
    public static Error Validation(string field, string message) =>
        new(Constants.ErrorCodes.ValidationError, message, field);

    /// <summary>
    /// Creates a not-found error for a resource.
    /// </summary>
    /// <param name="resourceName">Name of the resource type that was not found.</param>
    /// <returns>The constructed not-found <see cref="Error"/>.</returns>
    public static Error NotFound(string resourceName) =>
        new(Constants.ErrorCodes.NotFound, $"{resourceName} not found.");

    /// <summary>
    /// Creates a business-conflict error (maps to HTTP 409).
    /// </summary>
    /// <param name="message">Description of the conflict.</param>
    /// <returns>The constructed conflict <see cref="Error"/>.</returns>
    public static Error Conflict(string message) =>
        new(Constants.ErrorCodes.Conflict, message);

    /// <summary>
    /// Creates a business-rule violation error (maps to HTTP 422).
    /// </summary>
    /// <param name="message">Description of the violated rule.</param>
    /// <param name="field">Optional related field name.</param>
    /// <returns>The constructed business-rule <see cref="Error"/>.</returns>
    public static Error BusinessRule(string message, string? field = null) =>
        new(Constants.ErrorCodes.BusinessRuleViolation, message, field);
}
