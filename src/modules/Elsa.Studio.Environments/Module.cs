using Elsa.Studio.Abstractions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Components;

namespace Elsa.Studio.Environments;

public class Feature : FeatureBase
{
    private readonly IAppBarService _appBarService;

    public Feature(IAppBarService appBarService)
    {
        _appBarService = appBarService;
    }
    
    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        _appBarService.AddAppBarItem<EnvironmentPicker>();
        
        return ValueTask.CompletedTask;
    }
}