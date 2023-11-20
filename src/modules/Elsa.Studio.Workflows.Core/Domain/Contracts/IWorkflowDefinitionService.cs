using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// A service that can be used to manage workflow definitions.
/// </summary>
public interface IWorkflowDefinitionService
{
    /// <summary>
    /// Lists all workflow definitions.
    /// </summary>
    Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a workflow definition by its ID.
    /// </summary>
    Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = default, bool includeCompositeRoot = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds a workflow definition by its ID.
    /// </summary>
    Task<WorkflowDefinition?> FindByIdAsync(string id, bool includeCompositeRoot = false, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Finds all workflow definitions by their IDs.
    /// </summary>
    Task<IEnumerable<WorkflowDefinition>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Saves a workflow definition.
    /// </summary>
    Task<Result<WorkflowDefinition, ValidationErrors>> SaveAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a workflow definition.
    /// </summary>
    Task<bool> DeleteAsync(string definitionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes a workflow definition version.
    /// </summary>
    Task<bool> DeleteVersionAsync(string id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publishes a workflow definition.
    /// </summary>
    Task<WorkflowDefinition> PublishAsync(string definitionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retracts a workflow definition.
    /// </summary>
    Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(string definitionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes multiple workflow definitions.
    /// </summary>
    Task<long> BulkDeleteAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deletes multiple workflow definition versions.
    /// </summary>
    Task<long> BulkDeleteVersionsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Publishes multiple workflow definitions.
    /// </summary>
    Task<BulkPublishWorkflowDefinitionsResponse> BulkPublishAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retracts multiple workflow definitions.
    /// </summary>
    Task<BulkRetractWorkflowDefinitionsResponse> BulkRetractAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns true if a workflow definition with the specified name exists.
    /// </summary>
    Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Generates a unique name for a workflow definition.
    /// </summary>
    Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Creates a new workflow definition.
    /// </summary>
    Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports a workflow definition.
    /// </summary>
    Task<FileDownload> ExportDefinitionAsync(string definitionId, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Imports a workflow definition.
    /// </summary>
    Task<WorkflowDefinition> ImportDefinitionAsync(WorkflowDefinitionModel definitionModel, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Exports a set of workflow definitions.
    /// </summary>
    Task<FileDownload> BulkExportDefinitionsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the references of a workflow definition.
    /// </summary>
    Task<UpdateConsumingWorkflowReferencesResponse> UpdateReferencesAsync(string definitionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reverts the specified workflow definition to the specified version.
    /// </summary>
    Task RevertVersionAsync(string definitionId, int  version, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Executes a workflow definition.
    /// </summary>
    Task<string> ExecuteAsync(string definitionId, ExecuteWorkflowDefinitionRequest? request, CancellationToken cancellationToken = default);
}