using System.Net;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

public class RemoteWorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;
    private readonly IActivityIdGenerator _activityIdGenerator;
    private readonly IMediator _mediator;

    public RemoteWorkflowDefinitionService(IBackendConnectionProvider backendConnectionProvider, IActivityIdGenerator activityIdGenerator, IMediator mediator)
    {
        _backendConnectionProvider = backendConnectionProvider;
        _activityIdGenerator = activityIdGenerator;
        _mediator = mediator;
    }

    public async Task<ListWorkflowDefinitionsResponse> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        return await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .ListAsync(request, versionOptions, cancellationToken);
    }

    public async Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = default, bool includeCompositeRoot = false, CancellationToken cancellationToken = default)
    {
        return await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .GetByDefinitionIdAsync(definitionId, versionOptions, includeCompositeRoot, cancellationToken);
    }

    public async Task<WorkflowDefinition?> FindByIdAsync(string id, bool includeCompositeRoot = false, CancellationToken cancellationToken = default)
    {
        return await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .GetByIdAsync(id, includeCompositeRoot, cancellationToken);
    }

    public async Task<WorkflowDefinition> SaveAsync(SaveWorkflowDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var definition = await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .SaveAsync(request, cancellationToken);

        if (request.Publish == true)
            await _mediator.NotifyAsync(new WorkflowDefinitionPublished(definition), cancellationToken);

        return definition;
    }

    public async Task<bool> DeleteAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _backendConnectionProvider
                .GetApi<IWorkflowDefinitionsApi>()
                .DeleteAsync(definitionId, cancellationToken);

            await _mediator.NotifyAsync(new WorkflowDefinitionDeleted(definitionId), cancellationToken);
            return true;
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<bool> DeleteVersionAsync(string id, CancellationToken cancellationToken = default)
    {
        try
        {
            await _backendConnectionProvider
                .GetApi<IWorkflowDefinitionsApi>()
                .DeleteVersionAsync(id, cancellationToken);

            await _mediator.NotifyAsync(new WorkflowDefinitionVersionDeleted(id), cancellationToken);
            return true;
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public async Task<WorkflowDefinition> PublishAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var definition = await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .PublishAsync(definitionId, cancellationToken);

        await _mediator.NotifyAsync(new WorkflowDefinitionPublished(definition), cancellationToken);
        return definition;
    }

    public async Task<WorkflowDefinition> RetractAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var definition = await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .RetractAsync(definitionId, new RetractWorkflowDefinitionRequest(), cancellationToken);

        await _mediator.NotifyAsync(new WorkflowDefinitionRetracted(definition), cancellationToken);
        return definition;
    }

    public async Task<long> BulkDeleteAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var definitionIdList = definitionIds.ToList();
        var request = new BulkDeleteWorkflowDefinitionsRequest(definitionIdList);
        var response = await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .BulkDeleteAsync(request, cancellationToken);

        await _mediator.NotifyAsync(new WorkflowDefinitionsBulkDeleted(definitionIdList), cancellationToken);
        return response.Deleted;
    }

    public async Task<BulkPublishWorkflowDefinitionsResponse> BulkPublishAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var request = new BulkPublishWorkflowDefinitionsRequest(definitionIds);
        var response = await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .BulkPublishAsync(request, cancellationToken);

        await _mediator.NotifyAsync(new WorkflowDefinitionsBulkPublished(response.Published), cancellationToken);
        return response;
    }

    public async Task<BulkRetractWorkflowDefinitionsResponse> BulkRetractAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default)
    {
        var request = new BulkRetractWorkflowDefinitionsRequest(definitionIds);
        var response = await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .BulkRetractAsync(request, cancellationToken);

        await _mediator.NotifyAsync(new WorkflowDefinitionsBulkPublished(response.Retracted), cancellationToken);
        return response;
    }

    public async Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = default, CancellationToken cancellationToken = default)
    {
        var response = await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .GetIsNameUniqueAsync(name, definitionId, cancellationToken);

        return response.IsUnique;
    }

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

    public async Task<WorkflowDefinition> CreateNewDefinitionAsync(string name, string? description = default, CancellationToken cancellationToken = default)
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
                    ["id"] = _activityIdGenerator.GenerateId(),
                    ["type"] = "Elsa.Flowchart",
                    ["version"] = 1,
                    ["name"] = "Flowchart1"
                })
            }
        };

        return await SaveAsync(saveRequest, cancellationToken);
    }

    public async Task<FileDownload> ExportDefinitionAsync(string definitionId, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        var response = await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .ExportAsync(definitionId, versionOptions, cancellationToken);

        var fileName = $"workflow-definition-{definitionId}.json";

        if (response.Headers.TryGetValues("content-disposition", out var contentDispositionHeader)) // Only available if the Elsa Server exposes the "Content-Disposition" header.
        {
            var values = contentDispositionHeader?.ToList() ?? new List<string>();

            if (values.Count >= 2)
                fileName = values[1].Split('=')[1];
        }

        return new FileDownload(fileName, response.Content!);
    }

    public async Task<WorkflowDefinition> ImportDefinitionAsync(WorkflowDefinitionModel definition, CancellationToken cancellationToken = default)
    {
        return await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .ImportAsync(definition, cancellationToken);
    }
}

public record FileDownload(string? FileName, Stream Content);