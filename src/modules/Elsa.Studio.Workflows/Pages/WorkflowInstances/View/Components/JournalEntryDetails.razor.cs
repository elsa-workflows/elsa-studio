using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
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
}