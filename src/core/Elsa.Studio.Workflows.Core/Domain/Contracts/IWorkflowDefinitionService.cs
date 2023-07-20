using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Services;

namespace Elsa.Studio.Workflows.Domain.Contracts;

public interface IWorkflowDefinitionService
{
    Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = default, bool includeCompositeRoot = false, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> FindByIdAsync(string id, bool includeCompositeRoot = false, CancellationToken cancellationToken = default);
    Task<ListResponse<WorkflowDefinition>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition> SaveAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string definitionId, CancellationToken cancellationToken = default);
    Task<bool> DeleteVersionAsync(string id, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition> PublishAsync(string definitionId, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition> RetractAsync(string definitionId, CancellationToken cancellationToken = default);
    Task<long> BulkDeleteAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);
    Task<long> BulkDeleteVersionsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default);
    Task<BulkPublishWorkflowDefinitionsResponse> BulkPublishAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);
    Task<BulkRetractWorkflowDefinitionsResponse> BulkRetractAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default);
    Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = default, CancellationToken cancellationToken = default);
    Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default);
    Task<WorkflowDefinition> CreateNewDefinitionAsync(string name, string? description = default, CancellationToken cancellationToken = default);
    Task<FileDownload> ExportDefinitionAsync(string definitionId, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition> ImportDefinitionAsync(WorkflowDefinitionModel definition, CancellationToken cancellationToken = default);
    Task<UpdateConsumingWorkflowReferencesResponse> UpdateReferencesAsync(string definitionId, CancellationToken cancellationToken = default);
    Task RevertVersionAsync(string definitionId, int  version, CancellationToken cancellationToken = default);
}