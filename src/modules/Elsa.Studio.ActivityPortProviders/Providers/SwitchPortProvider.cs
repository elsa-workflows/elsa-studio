using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the Switch activity based on its cases.
/// </summary>
public class SwitchPortProvider : ActivityPortProviderBase
{
    public override bool GetSupportsActivityType(string activityType) => activityType == "Elsa.Switch";

    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var cases = GetCases(context.Activity);

        return cases.Select(x =>
        {
            var label = GetLabel(x);
            return new Port
            {
                Name = label,
                Type = PortType.Embedded,
                DisplayName = label
            };
        });
    }

    public override JsonObject? ResolvePort(string portName, PortProviderContext context)
    {
        var cases = GetCases(context.Activity);
        var @case = cases.FirstOrDefault(x => GetLabel(x) == portName);
        
        return @case != null ? GetActivity(@case) : null;
    }

    public override void AssignPort(string portName, JsonObject activity, PortProviderContext context)
    {
        var cases = GetCases(context.Activity).ToList();
        var @case = cases.FirstOrDefault(x => GetLabel(x) == portName);
        
        if (@case == null)
            return;

        SetActivity(@case, activity);
    }

    public override void ClearPort(string portName, PortProviderContext context)
    {
        var cases = GetCases(context.Activity).ToList();
        var @case = cases.FirstOrDefault(x => GetLabel(x) == portName);
        
        if (@case == null)
            return;

        SetActivity(@case, null);
    }

    private string GetLabel(JsonObject @case) => @case.GetProperty("label")?.GetValue<string>()!;
    private JsonObject? GetActivity(JsonObject @case) => @case.GetProperty("activity")?.AsObject();
    private void SetActivity(JsonObject @case, JsonObject? activity) => @case.SetProperty(activity, "activity");

    private static IEnumerable<JsonObject> GetCases(JsonObject switchActivity)
    {
        return switchActivity.GetProperty("cases")?.AsArray().AsEnumerable().Cast<JsonObject>() ?? new List<JsonObject>();
    }
}