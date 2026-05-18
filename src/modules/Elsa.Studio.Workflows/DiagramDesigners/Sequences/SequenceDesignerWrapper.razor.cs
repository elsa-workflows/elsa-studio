using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Designer.Components;
using Elsa.Studio.Workflows.Designer.Services;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.UI.Args;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Elsa.Studio.Workflows.DiagramDesigners.Sequences;

/// <summary>
/// A wrapper around the Sequence designer component.
/// </summary>
public partial class SequenceDesignerWrapper
{
    [Parameter] public JsonObject Sequence { get; set; } = null!;
    [Parameter] public IDictionary<string, ActivityStats>? ActivityStats { get; set; }
    [Parameter] public bool IsReadOnly { get; set; }
    [Parameter] public EventCallback<JsonObject> ActivitySelected { get; set; }
    [Parameter] public EventCallback<JsonObject> ActivityUpdated { get; set; }
    [Parameter] public EventCallback<ActivityEmbeddedPortSelectedArgs> ActivityEmbeddedPortSelected { get; set; }
    [Parameter] public EventCallback<JsonObject> ActivityDoubleClick { get; set; }
    [Parameter] public EventCallback GraphUpdated { get; set; }

    [CascadingParameter] private DragDropManager DragDropManager { get; set; } = null!;
    [Inject] private IIdentityGenerator IdentityGenerator { get; set; } = null!;
    [Inject] private IActivityNameGenerator ActivityNameGenerator { get; set; } = null!;

    private SequenceFlowDesigner? Designer { get; set; }
    private string? _lastSequenceId;

    public async Task LoadSequenceAsync(JsonObject activity, IDictionary<string, ActivityStats>? activityStats = null)
    {
        var sequence = activity.GetTypeName() == "Elsa.Sequence" ? activity : activity.FindActivitiesContainer();
        if (sequence == null)
            return;

        var id = sequence.GetId();
        if (id == _lastSequenceId && ReferenceEquals(sequence, Sequence))
            return;

        _lastSequenceId = id;
        Sequence = sequence;
        ActivityStats = activityStats;

        if (Designer is not null)
            await Designer.LoadSequenceAsync(sequence, activityStats);
    }

    public async Task UpdateActivityAsync(string id, JsonObject activity)
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Cannot update activity because the designer is read-only.");

        if (activity == Sequence) return;

        if (Designer is not null)
            await Designer.UpdateActivityAsync(id, activity);
    }

    public async Task UpdateActivityStatsAsync(string id, ActivityStats stats)
    {
        if (Designer is not null)
            await Designer.UpdateActivityStatsAsync(id, stats);
    }

    public async Task SelectActivityAsync(string id)
    {
        if (Designer is not null)
            await Designer.SelectActivityAsync(id);
    }

    public async Task<JsonObject> ReadRootActivityAsync()
    {
        if (Designer is not null)
            return await Designer.ReadSequenceAsync();

        return Sequence;
    }

    public async Task ZoomToFitAsync()
    {
        if (Designer is not null)
            await Designer.ZoomToFitAsync();
    }

    public async Task CenterContentAsync()
    {
        if (Designer is not null)
            await Designer.CenterContentAsync();
    }

    public async Task AutoLayoutAsync()
    {
        if (Designer is not null)
            await Designer.AutoLayoutAsync();
    }

    private async Task AddNewActivityAsync(ActivityDescriptor activityDescriptor, double x, double y)
    {
        var newActivity = SequenceActivityFactory.CreateActivity(Sequence, activityDescriptor, IdentityGenerator, ActivityNameGenerator, x, y);

        if (Designer is not null)
            await Designer.AddActivityAsync(newActivity);

        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(newActivity);
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

        await AddNewActivityAsync(activityDescriptor, e.PageX, e.PageY);
    }

    private async Task OnCanvasSelected()
    {
        if (ActivitySelected.HasDelegate)
            await ActivitySelected.InvokeAsync(Sequence);
    }
}
