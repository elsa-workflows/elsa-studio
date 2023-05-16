using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Workflows.Contracts;
using Refit;

namespace Elsa.Studio.Workflows.Services;

public class DefaultWorkflowDefinitionService : IWorkflowDefinitionService
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;

    public DefaultWorkflowDefinitionService(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }

    public async Task<ListWorkflowDefinitionsResponse> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default)
    {
        var serverUrl = _backendConnectionProvider.Url.ToString();
        var api = RestService.For<IWorkflowDefinitionsApi>(serverUrl);
        return await api.ListAsync(request, versionOptions, cancellationToken);
    }
}