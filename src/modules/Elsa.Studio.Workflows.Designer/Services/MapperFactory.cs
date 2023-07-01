using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Extensions;

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
        var descriptors = await _activityRegistry.GetDictionaryAsync(cancellationToken);
        return new ActivityMapper(descriptors, _activityPortService, _activityDisplaySettingsRegistry);
    }
}