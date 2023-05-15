using Elsa.Dashboard.Contracts;

namespace Elsa.Dashboard.Shell.Tasks;

public class InitializeModulesStartupTask : IStartupTask
{
    private readonly IModuleService _moduleService;

    public InitializeModulesStartupTask(IModuleService moduleService)
    {
        _moduleService = moduleService;
    }
    
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await _moduleService.InitializeModulesAsync(cancellationToken);
    }
}