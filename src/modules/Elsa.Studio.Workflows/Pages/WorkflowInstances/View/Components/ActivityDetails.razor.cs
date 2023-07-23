using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class ActivityDetails
{
    [Parameter] public int VisiblePaneHeight { get; set; }   
    [Parameter] public JsonObject Activity { get; set; } = default!;
}