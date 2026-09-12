using System.Text.Json.Nodes;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>
/// An <see cref="IBpmnInterchangeService"/> whose document endpoints answer with results a test queues up — the last
/// one repeating once the queue runs dry — and record every call; analyze, import and export throw to flag an
/// unexpected call.
/// </summary>
internal sealed class FakeBpmnDocumentService : IBpmnInterchangeService
{
    private readonly List<Result<BpmnDocumentRevision, BpmnDocumentFailure>> _getResults = [];
    private readonly List<Result<BpmnDocumentSaveResult, BpmnDocumentFailure>> _putResults = [];

    /// <summary>Every document the service was asked to PUT, as it was sent, with the If-Match it carried.</summary>
    public List<(string DefinitionId, JsonObject Document, string IfMatch)> Puts { get; } = [];

    public int GetCount { get; private set; }

    /// <summary>When set, every GET throws it, as a transport failure would.</summary>
    public Exception? GetException { get; set; }

    public FakeBpmnDocumentService ReturnsDocument(JsonObject document, string eTag) => ReturnsOnGet(new(new BpmnDocumentRevision(document, eTag)));
    public FakeBpmnDocumentService RefusesGet(BpmnDocumentFailureReason reason, string message = "refused") => ReturnsOnGet(new(new BpmnDocumentFailure(reason, message)));
    public FakeBpmnDocumentService AcceptsPut(string newETag) => ReturnsOnPut(new(new BpmnDocumentSaveResult(new BpmnImportResultModel(), newETag)));
    public FakeBpmnDocumentService RefusesPut(BpmnDocumentFailureReason reason, string message = "refused") => ReturnsOnPut(new(new BpmnDocumentFailure(reason, message)));

    public Task<Result<BpmnDocumentRevision, BpmnDocumentFailure>> GetDocumentAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        if (GetException != null)
            throw GetException;

        return Task.FromResult(Next(_getResults, GetCount++));
    }

    public Task<Result<BpmnDocumentSaveResult, BpmnDocumentFailure>> PutDocumentAsync(string definitionId, JsonObject document, string eTag, CancellationToken cancellationToken = default)
    {
        Puts.Add((definitionId, (JsonObject)document.DeepClone(), eTag));
        return Task.FromResult(Next(_putResults, Puts.Count - 1));
    }

    public Task<Result<BpmnImportAnalysisModel, ValidationErrors>> AnalyzeAsync(Stream content, string fileName, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Result<BpmnImportResultModel, ValidationErrors>> ImportAsync(Stream content, string fileName, string? definitionId, string? name, string? processId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Result<FileDownload, BpmnExportFailure>> ExportAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();

    private FakeBpmnDocumentService ReturnsOnGet(Result<BpmnDocumentRevision, BpmnDocumentFailure> result)
    {
        _getResults.Add(result);
        return this;
    }

    private FakeBpmnDocumentService ReturnsOnPut(Result<BpmnDocumentSaveResult, BpmnDocumentFailure> result)
    {
        _putResults.Add(result);
        return this;
    }

    private static T Next<T>(List<T> results, int index) =>
        results.Count == 0 ? throw new InvalidOperationException("No result was queued for this call.") : results[Math.Min(index, results.Count - 1)];
}
