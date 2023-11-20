using System.Net;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Notifications;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A workflow definition service that uses a remote backend to retrieve workflow definitions.
/// </summary>
public class RemoteWorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IRemoteBackendApiClientProvider _remoteBackendApiClientProvider;
    private readonly IIdentityGenerator _identityGenerator;
    private readonly IMediator _mediator;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteWorkflowDefinitionService"/> class.
    /// </summary>
    public RemoteWorkflowDefinitionService(IRemoteBackendApiClientProvider remoteBackendApiClientProvider, IIdentityGenerator identityGenerator, IMediator mediator)
    {
        _remoteBackendApiClientProvider = remoteBackendApiClientProvider;
        _identityGenerator = identityGenerator;
        _mediator = mediator;
    }

    /// <inheritdoc />
    public async Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.ListAsync(request, versionOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = default, bool includeCompositeRoot = false, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.GetByDefinitionIdAsync(definitionId, versionOptions, includeCompositeRoot, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition?> FindByIdAsync(string id, bool includeCompositeRoot = false, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.GetByIdAsync(id, includeCompositeRoot, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinition>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var response = await api.GetManyByIdAsync(ids.ToList(), true, cancellationToken);
        return response.Items;
    }

    /// <inheritdoc />
    public async Task<Result<WorkflowDefinition, ValidationErrors>> SaveAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);

        try
        {
            var definition = await api.SaveAsync(request, cancellationToken);

            if (request.Publish == true)
                await _mediator.NotifyAsync(new WorkflowDefinitionPublished(definition), cancellationToken);

            return new(definition);
        }
        catch (ValidationApiException e)
        {
            var errors = e.GetValidationErrors();
            return new(errors);
        }
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> PublishAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var definition = await api.PublishAsync(definitionId, new PublishWorkflowDefinitionRequest(), cancellationToken);
        await _mediator.NotifyAsync(new WorkflowDefinitionPublished(definition), cancellationToken);
        return definition;
    }

    /// <inheritdoc />
    public async Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await GetApiAsync(cancellationToken);
            var definition = await api.RetractAsync(definitionId, new RetractWorkflowDefinitionRequest(), cancellationToken);

            await _mediator.NotifyAsync(new WorkflowDefinitionRetracted(definition), cancellationToken);
            return new(definition);
        }
        catch (ValidationApiException e)
        {
            var errors = e.GetValidationErrors();
            return new(errors);
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);

        try
        {
            await api.DeleteAsync(definitionId, cancellationToken);
            await _mediator.NotifyAsync(new WorkflowDefinitionDeleted(definitionId), cancellationToken);
            return true;
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteVersionAsync(string id, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);

        try
        {
            await api.DeleteVersionAsync(id, cancellationToken);
            await _mediator.NotifyAsync(new WorkflowDefinitionVersionDeleted(id), cancellationToken);
            return true;
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        var request = new BulkDeleteWorkflowDefinitionsRequest(definitionIdList);
        var api = await GetApiAsync(cancellationToken);
        var response = await api.BulkDeleteAsync(request, cancellationToken);

        await _mediator.NotifyAsync(new WorkflowDefinitionsBulkDeleted(definitionIdList), cancellationToken);
        return response.Deleted;
    }

    /// <inheritdoc />
    public async Task<long> BulkDeleteVersionsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        var request = new BulkDeleteWorkflowDefinitionVersionsRequest(idList);
        var api = await GetApiAsync(cancellationToken);
        var response = await api.BulkDeleteVersionsAsync(request, cancellationToken);

        await _mediator.NotifyAsync(new WorkflowDefinitionVersionsBulkDeleted(idList), cancellationToken);
        return response.Deleted;
    }

    /// <inheritdoc />
    public async Task<BulkPublishWorkflowDefinitionsResponse> BulkPublishAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var request = new BulkPublishWorkflowDefinitionsRequest(definitionIds);
        var api = await GetApiAsync(cancellationToken);
        var response = await api.BulkPublishAsync(request, cancellationToken);

        await _mediator.NotifyAsync(new WorkflowDefinitionsBulkPublished(response.Published), cancellationToken);
        return response;
    }

    /// <inheritdoc />
    public async Task<BulkRetractWorkflowDefinitionsResponse> BulkRetractAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var request = new BulkRetractWorkflowDefinitionsRequest(definitionIds);
        var api = await GetApiAsync(cancellationToken);
        var response = await api.BulkRetractAsync(request, cancellationToken);

        await _mediator.NotifyAsync(new WorkflowDefinitionsBulkPublished(response.Retracted), cancellationToken);
        return response;
    }

    /// <inheritdoc />
    public async Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = default, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var response = await api.GetIsNameUniqueAsync(name, definitionId, cancellationToken);

        return response.IsUnique;
    }

    /// <inheritdoc />
    public async Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 100;
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            var name = $"Workflow {++attempt}";
            var isUnique = await GetIsNameUniqueAsync(name, null, cancellationToken);

            if (isUnique)
                return name;
        }

        throw new Exception($"Failed to generate a unique workflow name after {maxAttempts} attempts.");
    }

    /// <inheritdoc />
    public async Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description = default, CancellationToken cancellationToken = default)
    {
        var saveRequest = new SaveWorkflowDefinitionRequest
        {
            Model = new WorkflowDefinitionModel
            {
                Name = name,
                Description = description,
                Version = 1,
                ToolVersion = ToolVersion.Version,
                IsLatest = true,
                IsPublished = false,
                Root = new JsonObject(new Dictionary<string, JsonNode?>
                {
                    ["id"] = _identityGenerator.GenerateId(),
                    ["type"] = "Elsa.Flowchart",
                    ["version"] = 1,
                    ["name"] = "Flowchart1"
                })
            }
        };

        return await SaveAsync(saveRequest, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileDownload> ExportDefinitionAsync(string definitionId, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var response = await api.ExportAsync(definitionId, versionOptions, cancellationToken);
        var fileName = response.GetDownloadedFileNameOrDefault($"workflow-definition-{definitionId}.json");

        return new FileDownload(fileName, response.Content!);
    }

    /// <inheritdoc />
    public async Task<WorkflowDefinition> ImportDefinitionAsync(WorkflowDefinitionModel definitionModel, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.ImportAsync(definitionModel, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileDownload> BulkExportDefinitionsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var request = new BulkExportWorkflowDefinitionsRequest(ids.ToArray());
        var response = await api.BulkExportAsync(request, cancellationToken);
        var fileName = response.GetDownloadedFileNameOrDefault("workflow-definitions.zip");
        return new FileDownload(fileName, response.Content!);
    }

    /// <inheritdoc />
    public async Task<UpdateConsumingWorkflowReferencesResponse> UpdateReferencesAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        return await api.UpdateReferencesAsync(definitionId, new UpdateConsumingWorkflowReferencesRequest(), cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevertVersionAsync(string definitionId, int version, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        await api.RevertVersionAsync(definitionId, version, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string definitionId, ExecuteWorkflowDefinitionRequest? request, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var response = await api.ExecuteAsync(definitionId, request, cancellationToken);
        var workflowInstanceId = response.Headers.GetValues("x-elsa-workflow-instance-id").First();
        return workflowInstanceId;
    }

    private async Task<IWorkflowDefinitionsApi> GetApiAsync(CancellationToken cancellationToken = default) => await _remoteBackendApiClientProvider.GetApiAsync<IWorkflowDefinitionsApi>(cancellationToken);
}