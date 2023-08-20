using Elsa.Studio.Contracts;

namespace Elsa.Studio.Shell.Tasks;

/// <summary>
/// Initializes the modules.
/// </summary>
public class InitializeModulesStartupTask : IStartupTask
{
    private readonly IModuleService _moduleService;

    /// <summary>
    /// Initializes a new instance of the <see cref="InitializeModulesStartupTask"/> class.
    /// </summary>
    public InitializeModulesStartupTask(IModuleService moduleService)
    {
        _moduleService = moduleService;
    }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await _moduleService.InitializeModulesAsync(cancellationToken);
    }
}