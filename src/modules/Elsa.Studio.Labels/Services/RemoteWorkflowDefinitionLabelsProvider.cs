using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Contracts;
using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Client;
using Elsa.Studio.Labels.Contracts;
using Elsa.Studio.Labels.Models;
using Refit;

namespace Elsa.Studio.WorkflowContexts.Services;

/// <summary>
/// Provides workflow contexts from a remote API.
/// </summary>
public class RemoteWorkflowDefinitionLabelsProvider : IWorkflowDefinitionLabelsProvider
{
    private readonly IBackendApiClientProvider _backendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteWorkflowDefinitionLabelsProvider"/> class.
    /// </summary>
    public RemoteWorkflowDefinitionLabelsProvider(IBackendApiClientProvider backendApiClientProvider)
    {
        _backendApiClientProvider = backendApiClientProvider;
    }


    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowDefinitionLabelDescriptor>> ListAsync( string workflowDefinitionId, CancellationToken cancellationToken = default)
    {
        var api = await _backendApiClientProvider.GetApiAsync<IWorkflowDefinitionLabelsApi>(cancellationToken);
        var response = await api.ListAsync( workflowDefinitionId, cancellationToken);
        return response.Items.Select( it=> new WorkflowDefinitionLabelDescriptor()
        {
            WorkflowDefinitionId = workflowDefinitionId,
            Name = it,
        });
    }
}