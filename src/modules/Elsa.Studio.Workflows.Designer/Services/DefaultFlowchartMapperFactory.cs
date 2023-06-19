using Elsa.Studio.Workflows.Core.Contracts;
using Elsa.Studio.Workflows.Core.Extensions;
using Elsa.Studio.Workflows.Designer.Contracts;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class DefaultFlowchartMapperFactory : IFlowchartMapperFactory
{
    private readonly IActivityRegistry _activityRegistry;

    public DefaultFlowchartMapperFactory(IActivityRegistry activityRegistry)
    {
        _activityRegistry = activityRegistry;
    }
    
    public async Task<IFlowchartMapper> CreateAsync(CancellationToken cancellationToken = default)
    {
        var descriptors = await _activityRegistry.GetDictionaryAsync(cancellationToken);
        return new DefaultFlowchartMapper(descriptors);
    }
}