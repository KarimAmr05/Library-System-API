using LibrarySystem.Shared.Constants;
using LibrarySystem.Shared.Results;
using Microsoft.AspNetCore.Mvc;

namespace LibrarySystem.API.Extensions;

/// <summary>
/// Maps <see cref="Result"/> failures onto HTTP responses at the API boundary.
/// Centralizing the mapping keeps controllers thin and status codes consistent
/// with the documented contract (400/401/403/404/409/422).
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Determines the documented HTTP status code for an error.
    /// </summary>
    /// <param name="error">The error to map.</param>
    /// <returns>The HTTP status code.</returns>
    public static int ToStatusCode(this Error error) => error.Code switch
    {
        ErrorCodes.ValidationError => StatusCodes.Status400BadRequest,
        ErrorCodes.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorCodes.Forbidden => StatusCodes.Status403Forbidden,
        ErrorCodes.NotFound => StatusCodes.Status404NotFound,
        ErrorCodes.Conflict => StatusCodes.Status409Conflict,
        ErrorCodes.BusinessRuleViolation => StatusCodes.Status422UnprocessableEntity,
        _ => StatusCodes.Status500InternalServerError
    };

    /// <summary>
    /// Builds a failure <see cref="ObjectResult"/> with the standard error payload.
    /// </summary>
    /// <param name="result">The failed result.</param>
    /// <param name="traceId">Correlation id from the current HTTP context.</param>
    /// <returns>An action result carrying the error envelope.</returns>
    public static ObjectResult ToProblemResult(this Result result, string? traceId) =>
        new(new ErrorResponse
        {
            Code = result.Error?.Code ?? ErrorCodes.InternalError,
            Message = result.Error?.Message ?? "Operation failed.",
            Details = [.. result.Errors
                .Where(e => e.Field is not null)
                .Select(e => new ErrorDetail(e.Field!, e.Message))],
            TraceId = traceId
        })
        {
            StatusCode = (result.Error ?? new Error(ErrorCodes.InternalError, "Internal error.")).ToStatusCode()
        };
}

/// <summary>
/// Standard error response payload returned by all endpoints on failure.
/// </summary>
public sealed class ErrorResponse
{
    /// <summary>Gets or sets the machine-readable error code.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Gets or sets the human-readable error message.</summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>Gets or sets optional field-level validation details.</summary>
    public IReadOnlyList<ErrorDetail> Details { get; init; } = Array.Empty<ErrorDetail>();

    /// <summary>Gets or sets the correlation/trace identifier for diagnostics.</summary>
    public string? TraceId { get; init; }
}

/// <summary>
/// A single field-level validation detail inside an error response.
/// </summary>
/// <param name="Field">Name of the invalid field.</param>
/// <param name="Message">Why the field is invalid.</param>
public sealed record ErrorDetail(string Field, string Message);
