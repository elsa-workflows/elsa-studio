using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Contracts;

/// <summary>
/// Runs the interactive BPMN import flow for a single <c>.bpmn</c> file: analyze, show the findings for
/// confirmation (prompting for a process id when the document declares more than one), then import.
/// </summary>
public interface IBpmnImportUiService
{
    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="file"/>'s name has a <c>.bpmn</c> extension,
    /// case-insensitively.
    /// </summary>
    bool IsBpmnFile(IBrowserFile file);

    /// <summary>
    /// Analyzes <paramref name="file"/>, shows its findings for confirmation, and imports it once confirmed.
    /// </summary>
    /// <param name="file">The <c>.bpmn</c> file to import.</param>
    /// <param name="definitionId">The workflow definition to update, or <see langword="null"/> to create a new one.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>
    /// The import outcome, or <see langword="null"/> when the user cancelled the confirmation dialog before anything
    /// was imported.
    /// </returns>
    Task<WorkflowImportResult?> ImportFileAsync(IBrowserFile file, string? definitionId, CancellationToken cancellationToken = default);
}
