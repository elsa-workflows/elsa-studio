using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// A service that imports workflow definitions.
/// </summary>
public interface IWorkflowDefinitionImporter
{
    /// <summary>
    /// Imports a set of files containing workflow definitions.
    /// </summary>
    Task<IEnumerable<WorkflowImportResult>> ImportFilesAsync(IReadOnlyList<IBrowserFile> files, ImportOptions? options = null);
}