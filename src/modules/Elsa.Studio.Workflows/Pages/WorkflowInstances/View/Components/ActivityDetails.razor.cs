using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class ActivityDetails
{
    public record ActivityExecutionRecordTableRow(int Number, ActivityExecutionRecord ActivityExecution);
    
    [Parameter] public int VisiblePaneHeight { get; set; }   
    [Parameter] public JsonObject Activity { get; set; } = default!;
    [Parameter] public ICollection<ActivityExecutionRecord> ActivityExecutions { get; set; } = new List<ActivityExecutionRecord>();

    private ActivityExecutionRecord? LastActivityExecution => ActivityExecutions.LastOrDefault();
    private IEnumerable<ActivityExecutionRecordTableRow> Items => ActivityExecutions.Select((x, i) => new ActivityExecutionRecordTableRow(i + 1, x));
    private ActivityExecutionRecord? SelectedItem { get; set; } = default!;

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