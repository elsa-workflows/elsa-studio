using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Refit;

namespace Elsa.Studio.Workflows.Client;

/// <summary>
/// Backend API for the BPMN interchange endpoints (<c>Elsa.Bpmn.Interchange</c>): analyzing, importing and
/// exporting BPMN 2.0 documents.
/// </summary>
public interface IBpmnInterchangeApi
{
    /// <summary>
    /// Reads a <c>.bpmn</c> document and reports the Info/Degraded/Dropped findings a read would produce, without
    /// persisting anything.
    /// </summary>
    [Multipart]
    [Post("/bpmn/analyze")]
    Task<BpmnImportAnalysisModel> AnalyzeAsync([AliasAs("file")] StreamPart file, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a <c>.bpmn</c> document and persists it as a draft workflow definition.
    /// </summary>
    [Multipart]
    [Post("/bpmn/import")]
    Task<BpmnImportResultModel> ImportAsync(
        [AliasAs("file")] StreamPart file,
        [AliasAs("DefinitionId")] string? definitionId,
        [AliasAs("Name")] string? name,
        [AliasAs("ProcessId")] string? processId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the BPMN 2.0 XML a workflow definition was imported from.
    /// </summary>
    [Get("/bpmn/definitions/{definitionId}/export")]
    Task<HttpResponseMessage> ExportAsync(string definitionId, CancellationToken cancellationToken = default);
}
