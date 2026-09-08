using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// The outcome of running every <c>.bpmn</c> file in a mixed file selection through the interactive BPMN import
/// flow, plus whichever files were not <c>.bpmn</c> and so were left for the caller to import through its own
/// (JSON/ZIP) path.
/// </summary>
/// <param name="Results">
/// One <see cref="WorkflowImportResult"/> per <c>.bpmn</c> file that was imported, in selection order. A file whose
/// findings dialog was cancelled before anything was imported contributes no entry.
/// </param>
/// <param name="OtherFiles">Every selected file that was not a <c>.bpmn</c> file, unmodified.</param>
public record BpmnImportBatch(IReadOnlyList<WorkflowImportResult> Results, IReadOnlyList<IBrowserFile> OtherFiles);
