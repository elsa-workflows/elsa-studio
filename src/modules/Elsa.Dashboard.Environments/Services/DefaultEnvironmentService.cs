using Elsa.Dashboard.Environments.Contracts;
using Elsa.Dashboard.Environments.Models;

namespace Elsa.Dashboard.Environments.Services;

public class DefaultEnvironmentService : IEnvironmentService
{
    private readonly IPrimaryServerClient _primaryServerClient;

    public DefaultEnvironmentService(IPrimaryServerClient primaryServerClient)
    {
        _primaryServerClient = primaryServerClient;
    }
    
    public async ValueTask<IEnumerable<WorkflowsEnvironment>> ListEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        var environments = await _primaryServerClient.ListEnvironmentsAsync(cancellationToken);
        return environments;
    }
}