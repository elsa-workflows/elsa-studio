using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;

namespace Elsa.Studio.Workflows.Tests.Support;

/// <summary>
/// An <see cref="IWorkflowDefinitionService"/> stub that lets a test control when an asynchronous name
/// uniqueness check completes and records how the create dialog invoked it. Every member other than
/// <see cref="GetIsNameUniqueAsync"/> and <see cref="CreateNewDefinitionAsync(string,string?,string?,Action{SaveWorkflowDefinitionRequest}?,CancellationToken)"/>
/// is unused by the tests that consume this stub and throws to flag an unexpected call.
/// </summary>
internal sealed class ControlledWorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly Queue<PendingValidation> _pendingValidations = new();

    public int CreateCallCount { get; private set; }
    public string? CreatedName { get; private set; }

    public PendingValidation EnqueueValidation()
    {
        var validation = new PendingValidation();
        lock (_pendingValidations)
            _pendingValidations.Enqueue(validation);
        return validation;
    }

    public async Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = null, CancellationToken cancellationToken = default)
    {
        PendingValidation validation;
        lock (_pendingValidations)
            validation = _pendingValidations.Dequeue();

        validation.Name = name;
        validation.Started.TrySetResult(true);
        return await validation.Result.Task.WaitAsync(cancellationToken);
    }

    public Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description, string? rootActivityTemplateKey, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default)
    {
        CreateCallCount++;
        CreatedName = name;
        return Task.FromResult(new Result<WorkflowDefinition, ValidationErrors>(new WorkflowDefinition
        {
            Name = name,
            Description = description
        }));
    }

    public sealed class PendingValidation
    {
        public string? Name { get; set; }
        public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<bool> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<IEnumerable<WorkflowDefinition>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ActivityNode?> FindSubgraphAsync(string id, string? parentNodeId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<GetPathSegmentsResponse?> GetPathSegmentsAsync(string id, string? childNodeId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> DeleteAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<bool> DeleteVersionAsync(WorkflowDefinitionVersion workflowDefinitionVersion, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<SaveWorkflowDefinitionResponse> PublishAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<long> BulkDeleteAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<long> BulkDeleteVersionsAsync(IEnumerable<WorkflowDefinitionVersion> workflowDefinitionVersions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<BulkPublishWorkflowDefinitionsResponse> BulkPublishAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<BulkRetractWorkflowDefinitionsResponse> BulkRetractAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description = null, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<FileDownload> ExportDefinitionAsync(string definitionId, VersionOptions? versionOptions = null, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<FileDownload> BulkExportDefinitionsAsync(IEnumerable<string> ids, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<UpdateConsumingWorkflowReferencesResponse> UpdateReferencesAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    public Task<ExecuteWorkflowResult> ExecuteAsync(string definitionId, ExecuteWorkflowDefinitionRequest? request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
}
