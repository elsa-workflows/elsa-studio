using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class MapperFactory : IMapperFactory
{
    private readonly IActivityRegistry _activityRegistry;
    private readonly IActivityPortService _activityPortService;
    private readonly IActivityDisplaySettingsRegistry _activityDisplaySettingsRegistry;

    public MapperFactory(IActivityRegistry activityRegistry, IActivityPortService activityPortService, IActivityDisplaySettingsRegistry activityDisplaySettingsRegistry)
    {
        _activityRegistry = activityRegistry;
        _activityPortService = activityPortService;
        _activityDisplaySettingsRegistry = activityDisplaySettingsRegistry;
    }
    
    public async Task<IFlowchartMapper> CreateFlowchartMapperAsync(CancellationToken cancellationToken = default)
    {
        var activityMapper = await CreateActivityMapperAsync(cancellationToken);
        return new FlowchartMapper(activityMapper);
    }

    public async Task<IActivityMapper> CreateActivityMapperAsync(CancellationToken cancellationToken = default)
    {
        await _activityRegistry.EnsureLoadedAsync(cancellationToken);
        return new ActivityMapper(_activityRegistry, _activityPortService, _activityDisplaySettingsRegistry);
    }
}