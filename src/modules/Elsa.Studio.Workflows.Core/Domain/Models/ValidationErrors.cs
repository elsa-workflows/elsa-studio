using System.Net;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

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
/// <param name="Code">
/// The machine-readable code the error body carried, when the server sent one (see e.g.
/// <see cref="Bpmn.BpmnErrorCodes"/> for the BPMN-specific codes). <see langword="null"/> for an older server that
/// does not send a code yet, or for a failure that never carries one; callers must treat both the same way — as an
/// unrecognized code — and fall back to <see cref="Errors"/>'s message.
/// </param>
/// <param name="Data">
/// The structured data <see cref="Code"/> carries, when it carries any (today, only the missing capability names
/// and offending element ids of a BPMN import's capability refusal). <see langword="null"/> otherwise.
/// </param>
public record ValidationErrors(
    IReadOnlyCollection<ValidationError> Errors,
    HttpStatusCode? StatusCode = null,
    string? Code = null,
    BpmnCapabilityRefusal? Data = null);