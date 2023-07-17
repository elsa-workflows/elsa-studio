using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Humanizer;

namespace Elsa.Studio.Workflows.Domain.Providers;

public abstract class ActivityPortProviderBase : IActivityPortProvider
{
    public virtual double Priority => 0;
    public abstract bool GetSupportsActivityType(string activityType);

    public abstract IEnumerable<Port> GetPorts(PortProviderContext context);

    public virtual JsonObject? ResolvePort(string portName, PortProviderContext context)
    {
        var activity = context.Activity;
        var propName = portName.Camelize();
        return activity.GetProperty(propName)?.AsObject();
    }

    public virtual void AssignPort(string portName, JsonObject activity, PortProviderContext context)
    {
        var container = context.Activity;
        var propName = portName.Camelize();
        container.SetProperty(activity, propName);
    }
}