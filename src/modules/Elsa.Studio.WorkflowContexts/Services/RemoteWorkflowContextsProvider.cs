using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Contracts;
using Elsa.Api.Client.Resources.WorkflowExecutionContexts.Models;
using Elsa.Studio.WorkflowContexts.Contracts;

namespace Elsa.Studio.WorkflowContexts.Services;

/// <summary>
/// Provides workflow contexts from a remote API.
/// </summary>
public class RemoteWorkflowContextsProvider : IWorkflowContextsProvider
{
    private readonly IWorkflowContextProviderDescriptorsApi _workflowContextProviderDescriptorsApi;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteWorkflowContextsProvider"/> class.
    /// </summary>
    public RemoteWorkflowContextsProvider(IWorkflowContextProviderDescriptorsApi workflowContextProviderDescriptorsApi)
    {
        _workflowContextProviderDescriptorsApi = workflowContextProviderDescriptorsApi;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowContextProviderDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _workflowContextProviderDescriptorsApi.ListAsync(cancellationToken);
        return response.Items;
    }
}