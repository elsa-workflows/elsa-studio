using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Converters;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the FlowSendHttpRequest activity based on its supported status codes.
/// </summary>
public class FlowSendHttpRequestPortProvider : ActivityPortProviderBase
{
    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context) => context.ActivityDescriptor.TypeName is "Elsa.FlowSendHttpRequest";

    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var expectedStatusCodes = GetExpectedStatusCodes(context.Activity);

        foreach (var statusCode in expectedStatusCodes)
        {
            yield return new Port
            {
                Name = statusCode.ToString(),
                Type = PortType.Flow,
                DisplayName = statusCode.ToString()
            };
        }
        
        yield return new Port
        {
            Name = "Unmatched status code",
            Type = PortType.Flow,
            DisplayName = "Unmatched status code"
        };
        
        yield return new Port
        {
            Name = "Failed to connect",
            Type = PortType.Flow,
            DisplayName = "Failed to connect"
        };
        
        yield return new Port
        {
            Name = "Timeout",
            Type = PortType.Flow,
            DisplayName = "Timeout"
        };
        
        yield return new Port
        {
            Name = "Done",
            Type = PortType.Flow,
            DisplayName = "Done"
        };
    }

    private static IEnumerable<int> GetExpectedStatusCodes(JsonObject activity)
    {
        var options = CreateSerializerOptions();

        var wrappedInput = activity.GetProperty<WrappedInput>(options, "expectedStatusCodes") ?? new WrappedInput
        {
            TypeName = typeof(int[]).Name,
            Expression = Expression.CreateObject(JsonSerializer.Serialize(new[] { (int)HttpStatusCode.OK }, options))
        };
        
        var objectExpression = wrappedInput.Expression;
        return JsonSerializer.Deserialize<ICollection<int>>(objectExpression.Value!, options)!;
    }
    
    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        
        options.Converters.Add(new JsonStringToIntConverter());

        return options;
    }
}