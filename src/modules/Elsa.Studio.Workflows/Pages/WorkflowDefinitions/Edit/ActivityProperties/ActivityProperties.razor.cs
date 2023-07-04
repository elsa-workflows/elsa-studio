using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowDefinitions.Edit.ActivityProperties;

public partial class ActivityProperties
{
    [Parameter] public WorkflowDefinition? WorkflowDefinition { get; set; }
    [Parameter] public Activity? Activity { get; set; }
    [Parameter] public ActivityDescriptor? ActivityDescriptor { get; set; }
    [Parameter] public Func<Activity, Task>? OnActivityUpdated { get; set; }
    [Parameter] public int VisiblePaneHeight { get; set; }
}