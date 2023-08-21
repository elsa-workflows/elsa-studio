using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Humanizer;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class JournalEntryDetails
{
    [Parameter] public JournalEntry JournalEntry { get; set; } = default!;
    [Parameter] public int VisiblePaneHeight { get; set; }
    [CascadingParameter] public IDictionary<string, JsonObject> ActivityLookup { get; set; } = default!;

    private JsonObject GetActivity()
    {
        var activityId = JournalEntry.Record.ActivityId;
        return ActivityLookup[activityId];
    }

    private IDictionary<string, string?> ParsePayload(object? payload)
    {
        if (payload == null)
            return new Dictionary<string, string?>();

        if (payload is not JsonElement jsonElement)
            return new Dictionary<string, string?>
            {
                ["Payload"] = payload.ToString()
            };
        
        var properties = jsonElement.EnumerateObject().Where(x => !x.Name.StartsWith("_"));
        var result = new Dictionary<string, string?>();

        foreach (var property in properties)
        {
            var propertyName = property.Name.Pascalize();
            var propertyValue = property.Value;
            var propertyValueAsString = propertyValue.ToString();
            result[propertyName] = propertyValueAsString;
        }

        return result;
    }

    private static void Merge(IDictionary<string, string?> target, IDictionary<string, string?> input)
    {
        foreach (var (key, value) in input) target[key] = value;
    }
}