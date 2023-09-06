using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Expressions;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Converters;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Providers;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides ports for the FlowSendHttpRequest activity based on its supported status codes.
/// </summary>
public class DynamicOutcomesPortProvider : ActivityPortProviderBase
{
    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context) => GetDynamicOutcomesInput(context) != null;
    
    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var dynamicOutcomesInputDescriptor = GetDynamicOutcomesInput(context)!;
        var dynamicOutcomes = GetDynamicOutcomes(context.Activity, dynamicOutcomesInputDescriptor);

        foreach (var dynamicOutcome in dynamicOutcomes)
        {
            yield return new Port
            {
                Name = dynamicOutcome.ToString(),
                Type = PortType.Flow,
                DisplayName = dynamicOutcome.ToString()
            };
        }

        var dynamicOutcomesOptions = GetDynamicOutcomeOptions(dynamicOutcomesInputDescriptor);
        var fixedOutcomes = dynamicOutcomesOptions?.FixedOutcomes;

        if (fixedOutcomes == null) 
            yield break;
        
        foreach (var outcome in fixedOutcomes)
        {
            yield return new Port
            {
                Name = outcome,
                Type = PortType.Flow,
                DisplayName = outcome
            };
        }
    }

    private static DynamicOutcomesOptions? GetDynamicOutcomeOptions(InputDescriptor dynamicOutcomesInputDescriptor)
    {
        return dynamicOutcomesInputDescriptor.Options?.TryGetValue(nameof(DynamicOutcomesOptions), out var dynamicOutcomesOptions) == true 
            ? dynamicOutcomesOptions as DynamicOutcomesOptions 
            : default;
    }

    private static InputDescriptor? GetDynamicOutcomesInput(PortProviderContext context)
    {
        return context.ActivityDescriptor.Inputs.FirstOrDefault(x => x.UIHint == "DynamicOutcomes");
    }

    private static IEnumerable<int> GetDynamicOutcomes(JsonObject activity, PropertyDescriptor dynamicOutcomesInputDescriptor)
    {
        var options = CreateSerializerOptions();
        var dynamicOutcomesInputPropertyName = dynamicOutcomesInputDescriptor.Name;

        var wrappedInput = activity.GetProperty<WrappedInput>(options, dynamicOutcomesInputPropertyName) ?? new WrappedInput
        {
            TypeName = typeof(int[]).Name,
            Expression = new ObjectExpression
            {
                Value = JsonSerializer.Serialize(new[] { (int)HttpStatusCode.OK }, options)
            }
        };

        var objectExpression = (ObjectExpression)wrappedInput.Expression;
        return JsonSerializer.Deserialize<ICollection<int>>(objectExpression.Value!, options)!;
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        options.Converters.Add(new ExpressionJsonConverterFactory());
        options.Converters.Add(new JsonStringToIntConverter());

        return options;
    }
}