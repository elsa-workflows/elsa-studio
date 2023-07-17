using System.Text.Json;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the FlowSwitch activity based on its cases.
/// </summary>
public class FlowSwitchPortProvider : ActivityPortProviderBase
{
    public override bool GetSupportsActivityType(string activityType) => activityType == "Elsa.FlowSwitch";

    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        options.Converters.Add(new ExpressionJsonConverterFactory());
        
        var cases = context.Activity.GetProperty<List<FlowSwitchCase>>(options, "cases") ?? new List<FlowSwitchCase>();

        return cases.Select(x => new Port
        {
            Name = x.Label,
            Type = PortType.Flow,
            DisplayName = x.Label
        });
    }
}