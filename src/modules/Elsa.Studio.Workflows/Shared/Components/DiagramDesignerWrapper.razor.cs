using Elsa.Api.Client.Activities;
using Elsa.Studio.Workflows.Designer;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Shared.Components;

public partial class DiagramDesignerWrapper
{
    private IDiagramDesigner? _diagramDesigner;
    private Stack<ActivityPathSegment> _pathSegments = new();
    private bool _isProgressing;
    
    private List<BreadcrumbItem> _activityPath = new()
    {
        new("Flowchart1", href: "#", icon: ActivityIcons.Flowchart),
        new("ForEach1", href: "#", icon: @Icons.Material.Outlined.RepeatOne),
    };
    
    [Parameter]public Activity Activity { get; set; } = default!;
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public RenderFragment CustomToolbarItems { get; set; } = default!;
    [Parameter] public Func<Activity, Task>? ActivitySelected { get; set; }
    [Parameter] public Func<Task>? GraphUpdated { get; set; }
    [Inject] private IDiagramDesignerService DiagramDesignerService { get; set; } = default!;

    public Task<Activity> ReadActivityAsync()
    {
        return _diagramDesigner!.ReadRootActivityAsync();
    }
    
    public Task LoadActivityAsync(Activity activity)
    {
        return _diagramDesigner!.LoadRootActivity(activity);
    }
    
    public Task UpdateActivityAsync(string activityId, Activity activity)
    {
        return _diagramDesigner!.UpdateActivityAsync(activityId, activity);
    }

    protected override Task OnInitializedAsync()
    {
        _diagramDesigner = DiagramDesignerService.GetDiagramDesigner(Activity);
        
        return base.OnInitializedAsync();
    }

    private Activity GetCurrentActivity()
    {
        var currentActivity = Activity;
        
        foreach (var pathSegment in _pathSegments)
        {
            currentActivity = (Activity)currentActivity[pathSegment.PortName];
        }
        
        return currentActivity;
    }
    
}