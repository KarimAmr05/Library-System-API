using System.ComponentModel.DataAnnotations;
using LibrarySystem.Shared.Results;

namespace LibrarySystem.Business.Validators;

/// <summary>
/// Runs <see cref="DataAnnotations"/> validation on request DTOs and converts
/// failures into field-level result errors. Used as the first validation gate
/// before any business rules execute.
/// </summary>
public static class DtoValidator
{
    /// <summary>
    /// Validates an instance using its data annotations.
    /// </summary>
    /// <typeparam name="T">Type of the object being validated.</typeparam>
    /// <param name="instance">Instance to validate.</param>
    /// <returns>A failed result carrying per-field errors, or a successful result when valid.</returns>
    public static Result Validate<T>(T instance)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);

        var context = new ValidationContext(instance);
        var results = new List<ValidationResult>();
        if (Validator.TryValidateObject(instance, context, results, validateAllProperties: true))
        {
            return Result.Success();
        }

        var errors = results
            .SelectMany(r => r.MemberNames.DefaultIfEmpty(string.Empty),
                (r, member) => Error.Validation(member, r.ErrorMessage ?? "Invalid value."))
            .ToList();

        return Result.Invalid([.. errors]);
    }
}
