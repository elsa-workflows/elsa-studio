using System.Text.Json.Nodes;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.DiagramDesigners.Bpmn;

/// <summary>
/// One BPMN-imported workflow definition's <c>bpmnDefinitions</c> document, between the document <c>GET</c> that read
/// it and the document <c>PUT</c> that writes an edit back: the revision the server last returned, and a working copy
/// the "Performed by" section edits in place.
/// </summary>
/// <remarks>
/// <para>
/// <b>Why this exists.</b> A BPMN-imported definition's activity graph is derived from its document. An edit to a
/// bound activity has to be written into the document and saved through the document <c>PUT</c>, which re-imports
/// it; the ordinary workflow-definition save would rewrite the graph behind the document, which elsa-core then refuses
/// to read or export as stale. This class is the only thing that writes a binding, and it only ever writes one
/// through <see cref="SaveAsync"/>.
/// </para>
/// <para>
/// <b>Optimistic concurrency.</b> Every <c>PUT</c> carries, as <c>If-Match</c>, the <c>ETag</c> of the revision the
/// edit was made against. A refusal (<see cref="BpmnDocumentFailureReason.PreconditionFailed"/>) is reported, never
/// retried: the working copy stays as it is until the user reloads, which discards it.
/// </para>
/// <para>
/// <b>Subprocesses.</b> The document does not carry a subprocess's body, and elsa-core writes the XML from the document
/// alone, so saving a document that declares a subprocess would empty it. <see cref="SaveAsync"/> refuses such a
/// document before sending anything; see <see cref="BpmnDefinitionsDocument.FindSubProcessIds"/>.
/// </para>
/// </remarks>
public sealed class BpmnDocumentSession(IBpmnInterchangeService bpmnInterchangeService, string definitionId)
{
    private readonly SortedSet<string> _editedElementIds = new(StringComparer.Ordinal);
    private BpmnDocumentRevision? _revision;
    private Task? _loadTask;

    /// <summary>The workflow definition whose document this is.</summary>
    public string DefinitionId { get; } = definitionId;

    /// <summary>The working copy, with every unsaved edit applied, or <see langword="null"/> until a read succeeds.</summary>
    public JsonObject? Document { get; private set; }

    /// <summary>Whether a read is in flight.</summary>
    public bool IsLoading => _loadTask is { IsCompleted: false };

    /// <summary>Why the last read failed, or <see langword="null"/> when it did not.</summary>
    public BpmnDocumentFailure? LoadFailure { get; private set; }

    /// <summary>Why the last save failed, or <see langword="null"/> when it did not or an edit has been made since.</summary>
    public BpmnDocumentFailure? SaveFailure { get; private set; }

    /// <summary>The subprocesses the document declares; while there are any, <see cref="SaveAsync"/> refuses.</summary>
    public IReadOnlyList<string> SubProcessIds { get; private set; } = [];

    /// <summary>The elements whose binding the working copy changed since the document was last read or discarded.</summary>
    public IReadOnlyCollection<string> EditedElementIds => _editedElementIds;

    /// <summary>Whether the working copy differs from the revision the server last returned.</summary>
    public bool IsDirty => Document != null && _revision != null && !JsonNode.DeepEquals(Document, _revision.Document);

    /// <summary>Reads the document, unless a read has already started.</summary>
    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => _loadTask ??= LoadAsync(cancellationToken);

    /// <summary>Reads the document again, discarding the working copy and any unsaved edit in it.</summary>
    public Task ReloadAsync(CancellationToken cancellationToken = default) => _loadTask = LoadAsync(cancellationToken);

    /// <summary>The element of the working copy with <paramref name="elementId"/>, or <see langword="null"/>.</summary>
    public JsonObject? FindElement(string elementId) => Document == null ? null : BpmnDefinitionsDocument.FindElement(Document, elementId);

    /// <summary>
    /// Makes <paramref name="binding"/> the activity binding of the element with <paramref name="elementId"/> in the
    /// working copy. Nothing else in the document changes, and nothing is sent until <see cref="SaveAsync"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">The working copy has no such element.</exception>
    public void SetBinding(string elementId, JsonObject binding)
    {
        var element = FindElement(elementId) ?? throw new InvalidOperationException($"The BPMN document has no element '{elementId}'.");

        BpmnActivityBindingFormat.Attach(element, binding);
        _editedElementIds.Add(elementId);
        SaveFailure = null;
    }

    /// <summary>Throws away every unsaved edit, restoring the revision the server last returned.</summary>
    public void Discard()
    {
        if (_revision != null)
            Document = (JsonObject)_revision.Document.DeepClone();

        _editedElementIds.Clear();
        SaveFailure = null;
    }

    /// <summary>
    /// Writes the working copy back through the document <c>PUT</c>, against the revision it was edited from, then reads
    /// the document back so the session holds the server's own view and the new <c>ETag</c>.
    /// </summary>
    /// <returns>The server's result on success, or why the document was not saved.</returns>
    public async Task<Result<BpmnDocumentSaveResult, BpmnDocumentFailure>> SaveAsync(CancellationToken cancellationToken = default)
    {
        if (Document == null || _revision == null)
            return Refuse(new(BpmnDocumentFailureReason.Unknown, "The BPMN document has not been read, so there is nothing to save."));

        if (SubProcessIds.Count > 0)
        {
            return Refuse(new(
                BpmnDocumentFailureReason.SubProcessContentNotCarried,
                $"The BPMN document declares the subprocess(es) {string.Join(", ", SubProcessIds.Select(id => $"'{id}'"))}, whose content the document does not carry, so saving it would empty them."));
        }

        var result = await TryAsync(() => bpmnInterchangeService.PutDocumentAsync(DefinitionId, Document, _revision.ETag, cancellationToken));

        if (!result.IsSuccess)
            return Refuse(result.Failure!);

        await ReloadAsync(cancellationToken);
        return result;
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var result = await TryAsync(() => bpmnInterchangeService.GetDocumentAsync(DefinitionId, cancellationToken));

        _revision = result.Success;
        Document = (JsonObject?)_revision?.Document.DeepClone();
        SubProcessIds = _revision == null ? [] : BpmnDefinitionsDocument.FindSubProcessIds(_revision.Document);
        LoadFailure = result.Failure;
        SaveFailure = null;
        _editedElementIds.Clear();
    }

    /// <summary>
    /// Reports a request that never got an answer — the server unreachable, the connection dropped — as a failure the
    /// section shows, rather than an exception that would take the whole editor down with it.
    /// </summary>
    private static async Task<Result<T, BpmnDocumentFailure>> TryAsync<T>(Func<Task<Result<T, BpmnDocumentFailure>>> request)
    {
        try
        {
            return await request();
        }
        catch (HttpRequestException exception)
        {
            return new(new BpmnDocumentFailure(BpmnDocumentFailureReason.Unknown, exception.Message));
        }
    }

    private Result<BpmnDocumentSaveResult, BpmnDocumentFailure> Refuse(BpmnDocumentFailure failure)
    {
        SaveFailure = failure;
        return new(failure);
    }
}
