using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Contracts;
using Elsa.Api.Client.Resources.WorkflowActivationStrategies.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

public class RemoteWorkflowActivationStrategyService : IWorkflowActivationStrategyService
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;

    public RemoteWorkflowActivationStrategyService(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }
    
    public async Task<IEnumerable<WorkflowActivationStrategyDescriptor>> GetWorkflowActivationStrategiesAsync(CancellationToken cancellationToken = default)
    {
        var api = _backendConnectionProvider.GetApi<IWorkflowActivationStrategiesApi>();
        var response = await api.ListAsync(cancellationToken);
        return response.Items;
    }
}