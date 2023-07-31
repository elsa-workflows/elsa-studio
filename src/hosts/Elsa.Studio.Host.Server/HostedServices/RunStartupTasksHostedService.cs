using Elsa.Studio.Contracts;

namespace Elsa.Studio.Host.Server.HostedServices;

public class RunStartupTasksHostedService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public RunStartupTasksHostedService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var startupTasks = scope.ServiceProvider.GetServices<IStartupTask>();
        
        foreach (var startupTask in startupTasks)
            await startupTask.ExecuteAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}