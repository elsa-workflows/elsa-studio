using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

/// <summary>
/// Displays the details of an activity.
/// </summary>
public partial class ActivityDetails
{
    /// <summary>
    /// Represents a row in the table of activity executions.
    /// </summary>
    /// <param name="Number">The number of executions.</param>
    /// <param name="ActivityExecution">The activity execution.</param>
    public record ActivityExecutionRecordTableRow(int Number, ActivityExecutionRecord ActivityExecution);
    
    /// <summary>
    /// The height of the visible pane.
    /// </summary>
    [Parameter] public int VisiblePaneHeight { get; set; }
    
    /// <summary>
    /// The activity.
    /// </summary>
    [Parameter] public JsonObject Activity { get; set; } = default!;
    
    /// <summary>
    /// The activity execution records.
    /// </summary>
    [Parameter] public ICollection<ActivityExecutionRecord> ActivityExecutions { get; set; } = new List<ActivityExecutionRecord>();

    private ActivityExecutionRecord? LastActivityExecution => ActivityExecutions.LastOrDefault();
    private IEnumerable<ActivityExecutionRecordTableRow> Items => ActivityExecutions.Select((x, i) => new ActivityExecutionRecordTableRow(i + 1, x));
    private ActivityExecutionRecord? SelectedItem { get; set; } = default!;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        SelectedItem = null;
    }

    private Task OnActivityExecutionClicked(TableRowClickEventArgs<ActivityExecutionRecordTableRow> arg)
    {
        SelectedItem = arg.Item.ActivityExecution;
        StateHasChanged();
        return Task.CompletedTask;
    }
}