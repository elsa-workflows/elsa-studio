using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;
using Humanizer;

namespace Elsa.Studio.Workflows.PortProviders;

public class DefaultPortProvider : IPortProvider
{
    public double Priority => -1000; 

    public bool GetSupportsActivityType(string activityType) => true;

    public IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        return context.ActivityDescriptor.Ports.ToList();
    }

    public Activities? ResolvePort(string portName, PortProviderContext context)
    {
        var activity = context.Activity;
        var propName = portName.Camelize();
        var activities = (Activities?)activity.GetValueOrDefault(propName);

        return activities;
    }

    public void AssignPort(string portName, Activities? activities, PortProviderContext context)
    {
        var activity = context.Activity;
        var propName = portName.Camelize();
        activity[propName] = activities?.Match()!;
    }
}