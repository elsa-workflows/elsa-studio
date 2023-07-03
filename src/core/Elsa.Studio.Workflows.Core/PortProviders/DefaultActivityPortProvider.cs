using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Models;

namespace Elsa.Studio.Workflows.PortProviders;

public class DefaultActivityPortProvider : ActivityPortProviderBase
{
    public override double Priority => -1000; 

    public override bool GetSupportsActivityType(string activityType) => true;

    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        return context.ActivityDescriptor.Ports.ToList();
    }
}