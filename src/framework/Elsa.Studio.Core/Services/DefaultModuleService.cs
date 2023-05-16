using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

public class DefaultModuleService : IModuleService
{
    private readonly IEnumerable<IModule> _modules;

    public DefaultModuleService(IEnumerable<IModule> modules)
    {
        _modules = modules;
    }
    
    public IEnumerable<IModule> GetModules()
    {
        return _modules.ToList();
    }

    public async Task InitializeModulesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var module in GetModules()) 
            await module.InitializeAsync(cancellationToken);
    }
}