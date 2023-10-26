using Elsa.Studio.Workflows.Domain.Models;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ProblemDetails"/>.
/// </summary>
public static class ProblemDetailsExtensions
{
    /// <summary>
    /// Converts a <see cref="ProblemDetails"/> object to a <see cref="ValidationErrors"/> object.
    /// </summary>
    public static ValidationErrors ToValidationErrors(this ProblemDetails problemDetails)
    {
        var validationErrors = problemDetails.Errors.Select(x => new ValidationError(string.Join(", ", x.Value))).ToList();
        return new ValidationErrors(validationErrors);
    }
}