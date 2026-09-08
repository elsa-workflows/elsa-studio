using System.Net;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Represents a collection of validation errors.
/// </summary>
/// <param name="Errors">The individual error messages.</param>
/// <param name="StatusCode">
/// The HTTP status code the failure was raised with, when known. Callers that need to tell one kind of failure
/// from another by more than wording (e.g. a BPMN capability refusal, which is only ever a <c>422</c>) can gate on
/// this instead of matching the error text.
/// </param>
public record ValidationErrors(IReadOnlyCollection<ValidationError> Errors, HttpStatusCode? StatusCode = null);