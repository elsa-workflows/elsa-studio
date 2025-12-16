using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;

namespace Elsa.Studio.Environments.Tasks;

/// <summary>
/// Represents the load environments startup task.
/// </summary>
public class LoadEnvironmentsStartupTask(IEnvironmentsClient environmentsClient, IEnvironmentService environmentService) : IStartupTask
{
    /// <summary>
    /// Provides the value task.
    /// </summary>
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var response = await environmentsClient.ListEnvironmentsAsync(cancellationToken);
        environmentService.SetEnvironments(response.Environments, response.DefaultEnvironmentName);
    }
}