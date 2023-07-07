using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

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