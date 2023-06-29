using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;

namespace Elsa.Studio.Workflows.Services;

public class DefaultActivityPortService : IActivityPortService
{
    private readonly IEnumerable<IActivityPortProvider> _providers;

    public DefaultActivityPortService(IEnumerable<IActivityPortProvider> providers)
    {
        _providers = providers;
    }

    public IActivityPortProvider GetProvider(string activityType)
    {
        var provider = _providers.Where(x => x.GetSupportsActivityType(activityType)).MaxBy(x => x.Priority);
        return provider ?? throw new Exception($"No port provider found for activity type '{activityType}'.");
    }

    public IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var provider = GetProvider(context.ActivityDescriptor.TypeName);
        return provider.GetPorts(context);
    }
}