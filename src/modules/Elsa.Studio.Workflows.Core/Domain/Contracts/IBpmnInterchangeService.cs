using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// Analyzes, imports and exports BPMN 2.0 documents through the backend's <c>Elsa.Bpmn.Interchange</c> endpoints.
/// </summary>
public interface IBpmnInterchangeService
{
    /// <summary>
    /// Reads a <c>.bpmn</c> document and reports the Info/Degraded/Dropped findings a read would produce, without
    /// persisting anything.
    /// </summary>
    /// <param name="content">The BPMN 2.0 XML document.</param>
    /// <param name="fileName">The uploaded file's name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The analysis on success, or the server's error messages on failure.</returns>
    Task<Result<BpmnImportAnalysisModel, ValidationErrors>> AnalyzeAsync(Stream content, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reads a <c>.bpmn</c> document and persists it as a draft workflow definition.
    /// </summary>
    /// <param name="content">The BPMN 2.0 XML document.</param>
    /// <param name="fileName">The uploaded file's name.</param>
    /// <param name="definitionId">The workflow definition to update, or <see langword="null"/> to create a new one.</param>
    /// <param name="name">The workflow definition's display name, defaulting to the process's own BPMN name or id.</param>
    /// <param name="processId">The process to import when the document declares more than one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The imported definition's identity and analysis on success, or the server's error messages on failure.</returns>
    Task<Result<BpmnImportResultModel, ValidationErrors>> ImportAsync(
        Stream content,
        string fileName,
        string? definitionId,
        string? name,
        string? processId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the BPMN 2.0 XML a workflow definition was imported from, ready to download.
    /// </summary>
    /// <param name="definitionId">The workflow definition id to export.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The file to download on success, or the classified refusal reason on failure.</returns>
    Task<Result<FileDownload, BpmnExportFailure>> ExportAsync(string definitionId, CancellationToken cancellationToken = default);
}
