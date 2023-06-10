using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.Services;

public class DefaultWorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;

    public DefaultWorkflowDefinitionService(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }

    public async Task<ListWorkflowDefinitionsResponse> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions versionOptions, CancellationToken cancellationToken = default)
    {
        return await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .ListAsync(request, versionOptions, cancellationToken);
    }

    public async Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = default, bool includeCompositeRoot = false, CancellationToken cancellationToken = default)
    {
        return await _backendConnectionProvider
            .GetApi<IWorkflowDefinitionsApi>()
            .GetAsync(definitionId, versionOptions, includeCompositeRoot, cancellationToken);
    }
}