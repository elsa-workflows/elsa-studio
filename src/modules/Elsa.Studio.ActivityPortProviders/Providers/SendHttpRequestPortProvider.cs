using System.Net;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the FlowSendHttpRequest activity based on its supported status codes.
/// </summary>
public class SendHttpRequestPortProvider : ActivityPortProviderBase
{
    private const string UnmatchedStatusCodePortName = "Unmatched status code";

    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context) => context.ActivityDescriptor.TypeName is "Elsa.SendHttpRequest";

    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var cases = GetExpectedStatusCodes(context.Activity);

        foreach (var @case in cases)
        {
            var statusCode = GetStatusCode(@case);

            yield return new Port
            {
                Name = statusCode.ToString(),
                DisplayName = statusCode.ToString(),
                Type = PortType.Embedded,
            };
        }

        yield return new Port
        {
            Name = UnmatchedStatusCodePortName,
            DisplayName = "Unmatched status code",
            Type = PortType.Embedded,
        };
    }

    /// <inheritdoc />
    public override JsonObject? ResolvePort(string portName, PortProviderContext context)
    {
        if (portName == UnmatchedStatusCodePortName)
            return GetUnmatchedStatusCodeActivity(context.Activity);

        var cases = GetExpectedStatusCodes(context.Activity);
        var @case = cases.FirstOrDefault(x => GetStatusCode(x).ToString() == portName);

        return GetActivity(@case);
    }

    /// <inheritdoc />
    public override void AssignPort(string portName, JsonObject activity, PortProviderContext context)
    {
        var switchActivity = context.Activity;

        if (portName == UnmatchedStatusCodePortName)
        {
            SetUnmatchedStatusCodeActivity(switchActivity, activity);
            return;
        }

        var cases = GetExpectedStatusCodes(switchActivity).ToList();
        var @case = cases.FirstOrDefault(x => GetStatusCode(x).ToString() == portName);

        if (@case == null)
            return;

        SetActivity(@case, activity);
    }

    /// <inheritdoc />
    public override void ClearPort(string portName, PortProviderContext context)
    {
        if (portName == UnmatchedStatusCodePortName)
        {
            SetUnmatchedStatusCodeActivity(context.Activity, null);
            return;
        }

        var cases = GetExpectedStatusCodes(context.Activity).ToList();
        var @case = cases.FirstOrDefault(x => GetStatusCode(x).ToString() == portName);

        if (@case == null)
            return;

        SetActivity(@case, null);
    }

    private static IEnumerable<JsonObject> GetExpectedStatusCodes(JsonObject switchActivity)
    {
        return switchActivity.GetProperty("expectedStatusCodes")?.AsArray().AsEnumerable().Cast<JsonObject>() ?? new List<JsonObject>();
    }

    private static void SetExpectedStatusCodes(JsonObject switchActivity, ICollection<JsonObject> cases)
    {
        switchActivity.SetProperty(cases, "expectedStatusCodes");
    }

    private int GetStatusCode(JsonObject @case) => @case.GetProperty("statusCode")!.GetValue<int>();
    private JsonObject? GetActivity(JsonObject? @case) => @case?.GetProperty("activity")?.AsObject();
    private void SetActivity(JsonObject @case, JsonObject? activity) => @case.SetProperty(activity, "activity");
    private JsonObject? GetUnmatchedStatusCodeActivity(JsonObject httpRequestActivity) => httpRequestActivity.GetProperty("unmatchedStatusCode")?.AsObject();
    private void SetUnmatchedStatusCodeActivity(JsonObject httpRequestActivity, JsonObject? activity) => httpRequestActivity.SetProperty(activity, "unmatchedStatusCode");
}