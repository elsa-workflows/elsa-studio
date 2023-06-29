using System.Text.Json;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Models;
using Humanizer;

namespace Elsa.Studio.Workflows.PortProviders;

public class FlowSwitchPortProvider : IActivityPortProvider
{
    public double Priority => 0; 

    public bool GetSupportsActivityType(string activityType) => activityType == "Elsa.FlowSwitch";

    public IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        options.Converters.Add(new ExpressionJsonConverterFactory());
        
        var cases = context.Activity.TryGetValue("cases", () => new List<Case>(), options)!;
        
        //var cases = activity.Cases ?? new List<Case>();
        return cases.Select(x => new Port
        {
            Name = x.Label,
            Type = PortType.Flow,
            DisplayName = x.Label
        });
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