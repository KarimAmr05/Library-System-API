namespace LibrarySystem.Shared.Results;

/// <summary>
/// Generic result carrying a value on success. Used instead of throwing
/// exceptions for expected business failures such as missing entities or
/// invalid state transitions.
/// </summary>
/// <typeparam name="T">Type of the value carried on success.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class.
    /// </summary>
    /// <param name="value">The value produced by the operation, if any.</param>
    /// <param name="isSuccess">Whether the operation succeeded.</param>
    /// <param name="errors">Errors describing the failure, if any.</param>
    internal Result(T? value, bool isSuccess, IReadOnlyList<Error> errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value produced by the operation. Only valid when <see cref="Result.IsSuccess"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed on a failed result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            "The value of a failed result cannot be accessed. Check IsSuccess before reading Value.");
}
