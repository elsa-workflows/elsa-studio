using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Domain.Models.Bpmn;

/// <summary>
/// Reads the library-format <c>bpmnDefinitions</c> document the <c>bpmn/definitions/{definitionId}/document</c>
/// endpoints exchange (<c>Bpmn.Model</c> payload format 1.0.0; Studio's generated <c>BpmnDefinitions</c> type in the
/// Designer ClientLib's <c>src/bpmn/types.generated.ts</c> describes it). The document is held as a
/// <see cref="JsonObject"/> and edited in place, so everything Studio does not touch — foreign extensions, foreign
/// attributes, documentation, BPMN DI — goes back to the server exactly as it came.
/// </summary>
public static class BpmnDefinitionsDocument
{
    /// <summary>The <c>elementType</c> of an embedded subprocess, a transaction and an event subprocess alike.</summary>
    public const string SubProcessElementType = "subProcess";

    /// <summary>
    /// The BPMN element with <paramref name="elementId"/>, from any process's <c>elements</c>, or
    /// <see langword="null"/> when the document has none.
    /// </summary>
    /// <remarks>
    /// Only the elements of the document's own <c>&lt;process&gt;</c> elements are here. The body of a subprocess is
    /// not part of this document at all — <c>Bpmn.Interchange</c>'s reader carries it only in the work bindings it
    /// returns alongside, which the document endpoints do not send — so an element inside one is never found; see
    /// <see cref="FindSubProcessIds"/>.
    /// </remarks>
    public static JsonObject? FindElement(JsonObject document, string elementId) =>
        Elements(document).FirstOrDefault(element => element["elementId"]?.GetValue<string>() == elementId);

    /// <summary>
    /// The id of every subprocess — embedded, transaction or event subprocess — the document's processes declare.
    /// </summary>
    /// <remarks>
    /// Writing such a document back through the document <c>PUT</c> loses every one of these bodies: the body is not
    /// in the document (see <see cref="FindElement"/>), and elsa-core writes the XML from the document alone, which
    /// <c>BpmnXmlWriter</c> documents as writing each subprocess empty. A caller that would <c>PUT</c> must refuse
    /// while this is non-empty rather than discard the content silently.
    /// </remarks>
    public static IReadOnlyList<string> FindSubProcessIds(JsonObject document) =>
        Elements(document)
            .Where(element => element["elementType"]?.GetValue<string>() == SubProcessElementType)
            .Select(element => element["elementId"]?.GetValue<string>() ?? string.Empty)
            .ToList();

    private static IEnumerable<JsonObject> Elements(JsonObject document) =>
        (document["processes"] as JsonArray ?? [])
        .OfType<JsonObject>()
        .SelectMany(process => (process["elements"] as JsonArray ?? []).OfType<JsonObject>());
}
