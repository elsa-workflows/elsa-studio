using Elsa.Api.Client.Activities;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties.Tabs;

public partial class CommonTab
{
    [Parameter] public Activity? Activity { get; set; }
    [Parameter] public Func<Activity, Task>? OnActivityUpdated { get; set; }
    
    private async Task OnActivityIdChanged(string value)
    {
        Activity!.Id = value;
        
        if (OnActivityUpdated != null)
            await OnActivityUpdated(Activity);
    }
}