using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;

namespace Elsa.Studio.Environments.Tasks;

public class LoadEnvironmentsStartupTask : IStartupTask
{
    private readonly IPrimaryServerClient _primaryServerClient;
    private readonly IEnvironmentService _environmentService;

    public LoadEnvironmentsStartupTask(IPrimaryServerClient primaryServerClient, IEnvironmentService environmentService)
    {
        _primaryServerClient = primaryServerClient;
        _environmentService = environmentService;
    }

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var environments = (await _primaryServerClient.ListEnvironmentsAsync(cancellationToken)).ToList();
        _environmentService.Environments = environments;
    }
}