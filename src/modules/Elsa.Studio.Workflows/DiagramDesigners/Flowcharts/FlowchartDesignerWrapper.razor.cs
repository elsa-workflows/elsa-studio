using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Workflows.DiagramDesigners.Flowcharts;

public partial class FlowchartDesignerWrapper
{
    [Parameter] public Flowchart Flowchart { get; set; } = default!;
    [Parameter] public Func<Activity, Task>? OnActivitySelected { get; set; }
    [Parameter] public Func<Task>? OnGraphUpdated { get; set; }
    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;
    internal FlowchartDesigner Designer { get; set; } = default!;
    [Inject] private IActivityTypeService ActivityTypeService { get; set; } = default!;

    public async Task LoadFlowchartAsync(Flowchart flowchart)
    {
        Flowchart = flowchart;
        await Designer.LoadFlowchartAsync(flowchart);
    }
    
    public async Task UpdateActivityAsync(string id, Activity activity)
    {
        if (activity != Flowchart)
            await Designer.UpdateActivityAsync(id, activity);
    }

    public async Task<Activity> ReadRootActivityAsync() => await Designer.ReadFlowchartAsync();

    private int GetNextNumber(ActivityDescriptor activityDescriptor)
    {
        var count = Flowchart.Activities.Count(x => x.Type == activityDescriptor.TypeName);
        return count + 1;
    }

    private bool GetIdExists(string id) => Flowchart.Activities.Any(x => x.Id == id);

    private string GenerateNextId(ActivityDescriptor activityDescriptor)
    {
        var max = 100;
        var count = 0;

        while (count++ < max)
        {
            var nextId = $"{activityDescriptor.Name}{GetNextNumber(activityDescriptor)}";
            if (!GetIdExists(nextId))
                return nextId;
        }

        throw new Exception("Could not generate a unique ID.");
    }

    private async Task AddNewActivityAsync(ActivityDescriptor activityDescriptor, double x, double y)
    {
        var newActivityType = ActivityTypeService.ResolveType(activityDescriptor.TypeName);
        var newActivity = (Activity)Activator.CreateInstance(newActivityType)!;

        newActivity.Id = GenerateNextId(activityDescriptor);
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