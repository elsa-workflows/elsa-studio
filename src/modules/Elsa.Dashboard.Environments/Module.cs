using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Environments.Components;

namespace Elsa.Dashboard.Environments;

public class Module : IModule
{
    private readonly IAppBarService _appBarService;

    public Module(IAppBarService appBarService)
    {
        _appBarService = appBarService;
    }
    
    public ValueTask InitializeAsync(CancellationToken cancellationToken)
    {
        _appBarService.AddAppBarItem<EnvironmentPicker>();
        
        return ValueTask.CompletedTask;
    }
}