namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The capability names and offending element ids a BPMN import's <c>422</c> <see cref="BpmnErrorCodes.ImportCapabilityUnsupported"/>
/// refusal carries, read from the error envelope's <c>data.capabilities</c> and <c>data.elementIds</c> (see
/// <see cref="Elsa.Studio.Workflows.Domain.Extensions.ValidationApiExceptionExtensions.GetValidationErrorsFromContent"/>),
/// so the UI can list each separately instead of showing the raw message.
/// </summary>
public sealed record BpmnCapabilityRefusal(IReadOnlyList<string> CapabilityNames, IReadOnlyList<string> ElementIds);
