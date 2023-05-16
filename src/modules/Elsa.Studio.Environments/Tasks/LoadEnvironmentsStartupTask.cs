using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;

namespace Elsa.Studio.Environments.Tasks;

public class LoadEnvironmentsStartupTask : IStartupTask
{
    private readonly IEnvironmentsClient _environmentsClient;
    private readonly IEnvironmentService _environmentService;

    public LoadEnvironmentsStartupTask(IEnvironmentsClient environmentsClient, IEnvironmentService environmentService)
    {
        _environmentsClient = environmentsClient;
        _environmentService = environmentService;
    }

    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var response = await _environmentsClient.ListEnvironmentsAsync(cancellationToken);
        _environmentService.SetEnvironments(response.Environments, response.DefaultEnvironmentName);
    }
}