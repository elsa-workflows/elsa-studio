using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties.Tabs;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties;

public partial class ActivityPropertiesTabs
{
    //[Parameter] public Activity? Activity { get; set; }
    [Parameter] public Action<Activity>? OnActivityUpdated { get; set; }
    
    private Activity? Activity { get; set; }
    private InputsTab? InputsTab { get; set; }
    
    public void SetActivity(Activity activity)
    {
        //Activity = activity;
        //StateHasChanged();
        InputsTab?.SetActivity(activity);
    }
}