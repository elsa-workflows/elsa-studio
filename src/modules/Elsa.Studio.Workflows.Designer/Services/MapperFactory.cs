using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class MapperFactory(IActivityRegistry activityRegistry, IActivityPortService activityPortService, IActivityDisplaySettingsRegistry activityDisplaySettingsRegistry) : IMapperFactory
{
    public async Task<IFlowchartMapper> CreateFlowchartMapperAsync(CancellationToken cancellationToken = default)
    {
        var activityMapper = await CreateActivityMapperAsync(cancellationToken);
        return new FlowchartMapper(activityMapper);
    }

    public async Task<IActivityMapper> CreateActivityMapperAsync(CancellationToken cancellationToken = default)
    {
        await activityRegistry.EnsureLoadedAsync(cancellationToken);
        return new ActivityMapper(activityRegistry, activityPortService, activityDisplaySettingsRegistry);
    }
}