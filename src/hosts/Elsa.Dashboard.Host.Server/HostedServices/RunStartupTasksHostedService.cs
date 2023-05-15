using Elsa.Dashboard.Contracts;

namespace Elsa.Dashboard.Host.Server.HostedServices;

public class RunStartupTasksHostedService : IHostedService
{
    private readonly IEnumerable<IStartupTask> _startupTasks;

    public RunStartupTasksHostedService(IEnumerable<IStartupTask> startupTasks)
    {
        _startupTasks = startupTasks;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var startupTask in _startupTasks)
            await startupTask.ExecuteAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}