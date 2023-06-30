using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties.Tabs;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties;

public partial class ActivityPropertiesTabs
{
    [Parameter] public Activity? Activity { get; set; }
    [Parameter] public Func<Activity, Task>? OnActivityUpdated { get; set; }
    [Parameter] public int VisiblePaneHeight { get; set; }
}