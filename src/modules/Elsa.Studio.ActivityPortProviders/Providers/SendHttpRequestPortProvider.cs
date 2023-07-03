using System.Net;
using System.Text.Json;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Expressions;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Converters;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.PortProviders;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the FlowSendHttpRequest activity based on its supported status codes.
/// </summary>
public class SendHttpRequestPortProvider : ActivityPortProviderBase
{
    public override bool GetSupportsActivityType(string activityType) => activityType == "Elsa.FlowSendHttpRequest";

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
    }

    private static ICollection<int> GetExpectedStatusCodes(Activity activity)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        options.Converters.Add(new ExpressionJsonConverterFactory());
        options.Converters.Add(new JsonStringToIntConverter());

        var wrappedInput = activity.TryGetValue("expectedStatusCodes", () =>
        {
            var defaultStatusCodes = new[] { (int)HttpStatusCode.OK };
            var input = new WrappedInput
            {
                TypeName = typeof(int[]).Name,
                Expression = new ObjectExpression
                {
                    Value = JsonSerializer.Serialize(defaultStatusCodes, options)
                }
            };
            return input;
        }, options)!;

        var objectExpression = (ObjectExpression)wrappedInput.Expression;
        return JsonSerializer.Deserialize<ICollection<int>>(objectExpression.Value!, options)!;
    }
}