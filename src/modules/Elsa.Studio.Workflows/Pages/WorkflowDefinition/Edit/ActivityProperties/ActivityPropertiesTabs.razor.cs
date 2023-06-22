using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties.Tabs;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties;

public partial class ActivityPropertiesTabs
{
    [Parameter] public Activity? Activity { get; set; }
    [Parameter] public Action<Activity>? OnActivityUpdated { get; set; }
    
    private InputsTab? InputsTab { get; set; }
}