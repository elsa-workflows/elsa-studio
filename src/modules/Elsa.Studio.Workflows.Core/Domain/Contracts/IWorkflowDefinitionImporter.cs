using Refit;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// A service that imports workflow definitions.
/// </summary>
public interface IWorkflowDefinitionImporter
{
    /// <summary>
    /// Imports a set of files containing workflow definitions.
    /// </summary>
    Task<int> ImportAsync(IEnumerable<StreamPart> streamParts, CancellationToken cancellationToken = default);
}