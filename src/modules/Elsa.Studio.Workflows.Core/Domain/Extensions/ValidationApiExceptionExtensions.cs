using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
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
            return problemDetails.ToValidationErrors() with { StatusCode = e.StatusCode };

        return new ValidationErrors(new List<ValidationError> { new(e.ReasonPhrase ?? "The server responded with a Bad Request status code. That's all I know.") }, e.StatusCode);
    }

    /// <summary>
    /// Gets the validation errors from a <see cref="ApiException"/>.
    /// </summary>
    public static ValidationErrors GetValidationErrors(this ApiException e)
    {
        if (e is ValidationApiException validationApiException)
            return validationApiException.GetValidationErrors();

        var errors = GetValidationErrorsFromContent(e.Content);
        if (errors != null)
            return errors with { StatusCode = e.StatusCode };

        if (!string.IsNullOrWhiteSpace(e.Content))
            return new ValidationErrors(new List<ValidationError> { new(e.Content) }, e.StatusCode);

        return new ValidationErrors(new List<ValidationError> { new(e.ReasonPhrase ?? e.Message) }, e.StatusCode);
    }

    /// <summary>
    /// Parses a FastEndpoints-shaped error body (an <c>errors</c> object/array/string, or a <c>detail</c>/<c>title</c>/
    /// <c>message</c> fallback) into <see cref="ValidationErrors"/>, or <see langword="null"/> when <paramref name="content"/>
    /// is empty or does not parse as JSON. Shared with callers that have a raw response body rather than an
    /// <see cref="ApiException"/> to extract it from.
    /// </summary>
    /// <remarks>
    /// Also reads the envelope's additive <c>code</c> and <c>data</c> members (see e.g.
    /// <see cref="Elsa.Studio.Workflows.Domain.Models.Bpmn.BpmnErrorCodes"/> and <c>doc/wiki/bpmn-workflows.md</c> in
    /// elsa-core), so every caller shares one parser instead of a caller matching the message text itself. A body
    /// without a <c>code</c> — an older server, or a refusal that never carries one — leaves <see cref="ValidationErrors.Code"/>
    /// <see langword="null"/>, which every caller must treat as an unrecognized code and fall back to the message.
    /// </remarks>
    public static ValidationErrors? GetValidationErrorsFromContent(string? content)
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

            if (errors.Count == 0)
                return null;

            var code = TryGetProperty(root, "code", out var codeElement) && codeElement.ValueKind == JsonValueKind.String
                ? codeElement.GetString()
                : null;

            var data = TryGetProperty(root, "data", out var dataElement) ? GetCapabilityRefusal(dataElement) : null;

            return new ValidationErrors(errors, Code: code, Data: data);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Reads <paramref name="dataElement"/> into a <see cref="BpmnCapabilityRefusal"/> when it carries a
    /// <c>capabilities</c> and/or <c>elementIds</c> string array — the only shape any <c>data</c> member sends
    /// today (see <see cref="Elsa.Studio.Workflows.Domain.Models.Bpmn.BpmnErrorCodes.ImportCapabilityUnsupported"/>)
    /// — or <see langword="null"/> for any other shape, including a code that carries no <c>data</c> at all.
    /// </summary>
    private static BpmnCapabilityRefusal? GetCapabilityRefusal(JsonElement dataElement)
    {
        if (dataElement.ValueKind != JsonValueKind.Object)
            return null;

        var capabilityNames = TryGetProperty(dataElement, "capabilities", out var capabilitiesElement) ? GetStringArray(capabilitiesElement) : [];
        var elementIds = TryGetProperty(dataElement, "elementIds", out var elementIdsElement) ? GetStringArray(elementIdsElement) : [];

        return capabilityNames.Count == 0 && elementIds.Count == 0 ? null : new BpmnCapabilityRefusal(capabilityNames, elementIds);
    }

    private static IReadOnlyList<string> GetStringArray(JsonElement element) => element.ValueKind == JsonValueKind.Array
        ? element.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.String).Select(x => x.GetString()!).ToList()
        : [];

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
