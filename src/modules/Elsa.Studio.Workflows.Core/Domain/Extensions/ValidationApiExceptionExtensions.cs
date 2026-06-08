using Elsa.Studio.Workflows.Domain.Models;
using Refit;
using System.Text.Json;

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

    /// <summary>
    /// Gets the validation errors from a <see cref="ApiException"/>.
    /// </summary>
    public static ValidationErrors GetValidationErrors(this ApiException e)
    {
        if (e is ValidationApiException validationApiException)
            return validationApiException.GetValidationErrors();

        var errors = GetValidationErrorsFromContent(e.Content);
        return errors ?? new ValidationErrors(new List<ValidationError> { new(e.ReasonPhrase ?? e.Message) });
    }

    private static ValidationErrors? GetValidationErrorsFromContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        try
        {
            using var document = JsonDocument.Parse(content);
            var root = document.RootElement;
            var errors = new List<ValidationError>();

            if (TryGetProperty(root, "errors", out var errorsElement))
                errors.AddRange(GetErrorMessages(errorsElement).Select(x => new ValidationError(x)));

            if (errors.Count == 0)
                errors.AddRange(GetFallbackMessages(root).Select(x => new ValidationError(x)));

            return errors.Count > 0 ? new ValidationErrors(errors) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IEnumerable<string> GetErrorMessages(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                    foreach (var propertyMessage in GetErrorMessages(property.Value))
                        yield return propertyMessage;
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    foreach (var itemMessage in GetErrorMessages(item))
                        yield return itemMessage;
                break;
            case JsonValueKind.String:
                var message = element.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                    yield return message;
                break;
        }
    }

    private static IEnumerable<string> GetFallbackMessages(JsonElement root)
    {
        foreach (var propertyName in new[] { "detail", "title", "message" })
            if (TryGetProperty(root, propertyName, out var property) && property.ValueKind == JsonValueKind.String)
            {
                var message = property.GetString();
                if (!string.IsNullOrWhiteSpace(message))
                    yield return message;
            }
    }

    private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement property)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var candidate in element.EnumerateObject())
                if (string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property = candidate.Value;
                    return true;
                }
        }

        property = default;
        return false;
    }
}
