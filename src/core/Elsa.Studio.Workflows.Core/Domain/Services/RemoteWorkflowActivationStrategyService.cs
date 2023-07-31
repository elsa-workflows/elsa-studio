using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Contracts;
using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A workflow activation strategy service that uses a remote backend to retrieve workflow activation strategies.
/// </summary>
public class RemoteWorkflowActivationStrategyService : IWorkflowActivationStrategyService
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteWorkflowActivationStrategyService"/> class.
    /// </summary>
    public RemoteWorkflowActivationStrategyService(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WorkflowActivationStrategyDescriptor>> GetWorkflowActivationStrategiesAsync(CancellationToken cancellationToken = default)
    {
        var api = await _backendConnectionProvider.GetApiAsync<IWorkflowActivationStrategiesApi>(cancellationToken);
        var response = await api.ListAsync(cancellationToken);
        return response.Items;
    }
}