namespace LibrarySystem.Shared.Results;

/// <summary>
/// Non-generic result used for operations that either succeed or fail
/// without producing a value. Replaces exception-based control flow for
/// expected business failures.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="errors">Errors describing the failure, if any.</param>
    protected Result(bool isSuccess, IReadOnlyList<Error> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the collection of errors associated with a failed operation. Empty on success.
    /// </summary>
    public IReadOnlyList<Error> Errors { get; }

    /// <summary>
    /// Gets the primary (first) error of a failed operation, if any.
    /// </summary>
    public Error? Error => Errors.Count > 0 ? Errors[0] : null;

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful <see cref="Result"/>.</returns>
    public static Result Success() => new(true, Array.Empty<Error>());

    /// <summary>
    /// Creates a successful generic result carrying a value.
    /// </summary>
    /// <typeparam name="T">Type of the carried value.</typeparam>
    /// <param name="value">The value produced by the operation.</param>
    /// <returns>A successful <see cref="Result{T}"/>.</returns>
    public static Result<T> Success<T>(T value) => new(value, true, Array.Empty<Error>());

    /// <summary>
    /// Creates a failed result from one or more errors.
    /// </summary>
    /// <param name="errors">Errors describing the failure.</param>
    /// <returns>A failed <see cref="Result"/>.</returns>
    public static Result Failure(params Error[] errors) =>
        new(false, errors.Length == 0
            ? [new Error(Constants.ErrorCodes.BusinessRuleViolation, "Operation failed.")]
            : errors);

    /// <summary>
    /// Creates a failed generic result from one or more errors.
    /// </summary>
    /// <typeparam name="T">The value type of the result.</typeparam>
    /// <param name="errors">Errors describing the failure.</param>
    /// <returns>A failed <see cref="Result{T}"/>.</returns>
    public static Result<T> Failure<T>(params Error[] errors) =>
        new(default, false, errors.Length == 0
            ? [new Error(Constants.ErrorCodes.BusinessRuleViolation, "Operation failed.")]
            : errors);

    /// <summary>
    /// Creates a validation-failed result with one error per invalid field.
    /// </summary>
    /// <param name="validationErrors">Field-level validation errors.</param>
    /// <returns>A failed <see cref="Result"/> carrying validation details.</returns>
    public static Result Invalid(params Error[] validationErrors) =>
        new(false, validationErrors.Length > 0 ? validationErrors :
        [new Error(Constants.ErrorCodes.ValidationError, "Invalid request.")]);
}
