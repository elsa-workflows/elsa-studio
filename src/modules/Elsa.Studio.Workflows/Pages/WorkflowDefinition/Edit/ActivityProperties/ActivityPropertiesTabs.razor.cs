using Elsa.Api.Client.Activities;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinition.Edit.ActivityProperties;

public partial class ActivityPropertiesTabs
{
    [Parameter] public Activity? Activity { get; set; }
    [Parameter] public EventCallback<Activity> OnActivityUpdated { get; set; }
}