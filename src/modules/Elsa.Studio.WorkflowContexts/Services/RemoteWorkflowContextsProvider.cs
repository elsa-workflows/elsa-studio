using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Contracts;
using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.WorkflowContexts.Contracts;

namespace Elsa.Studio.WorkflowContexts.Services;

/// <summary>
/// Provides workflow contexts from a remote API.
/// </summary>
public class RemoteWorkflowContextsProvider : IWorkflowContextsProvider
{
    private readonly IRemoteBackendApiClientProvider _remoteBackendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteWorkflowContextsProvider"/> class.
    /// </summary>
    public RemoteWorkflowContextsProvider(IRemoteBackendApiClientProvider remoteBackendApiClientProvider)
    {
        _remoteBackendApiClientProvider = remoteBackendApiClientProvider;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowContextProviderDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var api = await _remoteBackendApiClientProvider.GetApiAsync<IWorkflowContextProviderDescriptorsApi>(cancellationToken);
        var response = await api.ListAsync(cancellationToken);
        return response.Items;
    }
}