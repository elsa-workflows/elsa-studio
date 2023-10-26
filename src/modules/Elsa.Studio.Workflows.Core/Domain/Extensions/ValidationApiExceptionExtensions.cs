using Elsa.Studio.Workflows.Domain.Models;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ValidationApiException"/>.
/// </summary>
public static class ValidationApiExceptionExtensions
{
    /// <summary>
    /// Gets the validation errors from a <see cref="ValidationApiException"/>.
    /// </summary>
    public static ValidationErrors GetValidationErrors(this ValidationApiException e)
    {
        var problemDetails = e.Content;

        if (problemDetails != null)
            return problemDetails.ToValidationErrors();

        return problemDetails?.ToValidationErrors() ?? new ValidationErrors(new List<ValidationError> { new(e.ReasonPhrase ?? "The server responded with a Bad Request status code. That's all I know.") });
    }
}