using DEDrake;
using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

public partial class FlowchartDesignerWrapper
{
    [Parameter] public Flowchart Flowchart { get; set; } = default!;
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public Func<Activity, Task>? OnActivitySelected { get; set; }
    [Parameter] public Func<Task>? OnGraphUpdated { get; set; }
    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;
    [Inject] private IActivityTypeService ActivityTypeService { get; set; } = default!;
    [Inject] private IActivityIdGenerator ActivityIdGenerator { get; set; } = default!;
    internal FlowchartDesigner Designer { get; set; } = default!;

    public async Task LoadFlowchartAsync(Flowchart flowchart)
    {
        Flowchart = flowchart;
        await Designer.LoadFlowchartAsync(flowchart);
    }
    
    public async Task UpdateActivityAsync(string id, Activity activity)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Cannot update activity because the designer is read-only.");
        
        if (activity != Flowchart)
            await Designer.UpdateActivityAsync(id, activity);
    }

    public async Task<Activity> ReadRootActivityAsync() => await Designer.ReadFlowchartAsync();

    private bool GetNameExists(string name) => Flowchart.Activities.Any(x => x.Id == name);

    private string GenerateNextName(ActivityDescriptor activityDescriptor)
    {
        var max = 100;
        var count = GetNextNumber(activityDescriptor);

        while (count++ < max)
        {
            var nextId = $"{activityDescriptor.Name}{count}";
            if (!GetNameExists(nextId))
                return nextId;
        }

        throw new Exception("Could not generate a unique name.");
    }
    
    private int GetNextNumber(ActivityDescriptor activityDescriptor)
    {
        return Flowchart.Activities.Count(x => x.Type == activityDescriptor.TypeName);
    }

    private async Task AddNewActivityAsync(ActivityDescriptor activityDescriptor, double x, double y)
    {
        var newActivityType = ActivityTypeService.ResolveType(activityDescriptor.TypeName);
        var newActivity = (Activity)Activator.CreateInstance(newActivityType)!;

        newActivity.Id = ActivityIdGenerator.GenerateId();
        newActivity.Name = GenerateNextName(activityDescriptor);
        newActivity.Type = activityDescriptor.TypeName;
        newActivity.Version = activityDescriptor.Version;

        newActivity.SetDesignerMetadata(new ActivityDesignerMetadata
        {
            Position = new Position(x, y)
        });
        
        // If the activity is a trigger and it's the first trigger on the flowchart, set the trigger property to true.
        if (activityDescriptor.Kind == ActivityKind.Trigger && Flowchart.Activities.All(activity => activity.GetCanStartWorkflow() != true))
            newActivity.SetCanStartWorkflow(true);

        await Designer.AddActivityAsync(newActivity);
        
        OnActivitySelected?.Invoke(newActivity);
    }

    private void OnDragOver(DragEventArgs e)
    {
        if (DragDropManager.Payload is not ActivityDescriptor)
        {
            e.DataTransfer.DropEffect = "none";
            return;
        }

        e.DataTransfer.DropEffect = "move";
    }

    private async Task OnDrop(DragEventArgs e)
    {
        if (DragDropManager.Payload is not ActivityDescriptor activityDescriptor)
            return;

        var x = e.PageX;
        var y = e.PageY;

        await AddNewActivityAsync(activityDescriptor, x, y);
    }

    private async Task OnCanvasSelected()
    {
        if (OnActivitySelected != null)
            await OnActivitySelected(Flowchart);
    }

    private async Task OnZoomToFitClick() => await Designer.ZoomToFitAsync();
    private async Task OnCenterContentClick() => await Designer.CenterContentAsync();
}