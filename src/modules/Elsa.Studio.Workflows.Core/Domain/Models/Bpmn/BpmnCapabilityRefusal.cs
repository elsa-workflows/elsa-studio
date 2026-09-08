namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// The capability names and offending element ids a BPMN import's <c>422</c> "missing host capabilities" refusal
/// carries, parsed out of the single sentence <c>BpmnInterchangeDocumentService.Import</c> raises in elsa-core, so
/// the UI can list each separately instead of showing the raw sentence.
/// </summary>
public sealed record BpmnCapabilityRefusal(IReadOnlyList<string> CapabilityNames, IReadOnlyList<string> ElementIds)
{
    private const string CapabilitiesPrefix = "does not declare the following BPMN host capabilities the document requires:";
    private const string ElementsPrefix = "Offending elements (combined across all missing capabilities above, not attributable to any one of them):";

    /// <summary>
    /// Parses <paramref name="message"/> into a <see cref="BpmnCapabilityRefusal"/> when it matches the capability
    /// refusal's known shape, or returns <see langword="null"/> for any other message (including the two export
    /// refusals, which use different wording, and unrelated failures).
    /// </summary>
    public static BpmnCapabilityRefusal? TryParse(string message)
    {
        var capabilitiesIndex = message.IndexOf(CapabilitiesPrefix, StringComparison.OrdinalIgnoreCase);
        var elementsIndex = message.IndexOf(ElementsPrefix, StringComparison.OrdinalIgnoreCase);

        if (capabilitiesIndex < 0 || elementsIndex < 0 || elementsIndex < capabilitiesIndex)
            return null;

        var capabilitiesText = message[(capabilitiesIndex + CapabilitiesPrefix.Length)..elementsIndex];
        var elementsText = message[(elementsIndex + ElementsPrefix.Length)..];

        var capabilityNames = SplitList(capabilitiesText);
        var elementIds = SplitList(elementsText);

        return capabilityNames.Count == 0 && elementIds.Count == 0 ? null : new BpmnCapabilityRefusal(capabilityNames, elementIds);
    }

    private static IReadOnlyList<string> SplitList(string text) => text
        .Trim().Trim('.')
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
