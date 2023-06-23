using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Workflows.DiagramEditors.Flowcharts;

public partial class FlowchartEditor // : IDiagramEditor, IDiagramEditorToolboxProvider
{
    private FlowchartDesigner _designer = default!;

    [Parameter] public Api.Client.Activities.Flowchart Flowchart { get; set; } = default!;
    [Parameter] public Func<Activity, Task>? OnActivitySelected { get; set; }
    [Parameter] public Func<Task>? OnGraphUpdated { get; set; }
    [CascadingParameter] public DragDropManager DragDropManager { get; set; } = default!;
    [Inject] private IActivityTypeService ActivityTypeService { get; set; } = default!;

    public async Task UpdateActivityAsync(Activity activity)
    {
        if (activity != Flowchart)
            await _designer.UpdateActivityAsync(activity);
    }

    public async Task<Activity> ReadRootActivityAsync() => await _designer.ReadFlowchartAsync();

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
            var nextId = $"{activityDescriptor}{GetNextNumber(activityDescriptor)}";
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

        await _designer.AddActivityAsync(newActivity);
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

    private async Task OnSelectedActivityUpdated(Activity activity)
    {
        await _designer.UpdateActivityAsync(activity);
    }

    private async Task OnCanvasSelected()
    {
        if (OnActivitySelected != null)
            await OnActivitySelected(Flowchart);
    }

    private async Task OnZoomToFitClick() => await _designer.ZoomToFitAsync();
    private async Task OnCenterContentClick() => await _designer.CenterContentAsync();
}