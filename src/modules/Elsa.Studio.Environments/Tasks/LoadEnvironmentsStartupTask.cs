using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;

namespace Elsa.Studio.Environments.Tasks;

public class LoadEnvironmentsStartupTask(IEnvironmentsClient environmentsClient, IEnvironmentService environmentService) : IStartupTask
{
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var response = await environmentsClient.ListEnvironmentsAsync(cancellationToken);
        environmentService.SetEnvironments(response.Environments, response.DefaultEnvironmentName);
    }
}