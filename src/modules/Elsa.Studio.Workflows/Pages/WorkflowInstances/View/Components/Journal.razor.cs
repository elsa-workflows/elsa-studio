using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class Journal
{
    [Inject] private IWorkflowInstanceService WorkflowInstanceService { get; set; } = default!;
    [Inject] private IActivityRegistry ActivityRegistry { get; set; } = default!;
    [Inject] private IActivityDisplaySettingsRegistry ActivityDisplaySettingsRegistry { get; set; } = default!;

    public WorkflowInstance WorkflowInstance { get; set; } = default!;
    private List<ExecutionLogRecord> ExecutionLogRecords { get; set; } = new();
    private int CurrentPage { get; set; } = 0;
    private int PageSize { get; set; } = 100;
    private IDictionary<string, ActivityDescriptor>? ActivityDescriptors { get; set; }
    private TimeMetricMode TimeMetricMode { get; set; } = TimeMetricMode.Relative;
    
    public async Task SetWorkflowInstanceAsync(WorkflowInstance workflowInstance)
    {
        WorkflowInstance = workflowInstance;
        CurrentPage = 0;
        ExecutionLogRecords.Clear();
        await EnsureActivityDescriptorsAsync();
        await GetMoreDataAsync();
    }
    
    private async Task GetMoreDataAsync()
    {
        var response = await WorkflowInstanceService.GetJournalAsync(WorkflowInstance.Id, CurrentPage, PageSize);

        ExecutionLogRecords.AddRange(response.Items);
        CurrentPage++;

        StateHasChanged();
    }
    
    private async Task EnsureActivityDescriptorsAsync()
    {
        if (ActivityDescriptors != null)
            return;

        var activities = await ActivityRegistry.ListAsync();
        ActivityDescriptors = activities.ToDictionary(x => x.TypeName);
    }

    private TimeSpan GetTimeMetric(ExecutionLogRecord current, ExecutionLogRecord? previous)
    {
        return TimeMetricMode switch
        {
            TimeMetricMode.Relative => previous == null ? TimeSpan.Zero : current.Timestamp - previous.Timestamp,
            TimeMetricMode.Absolute => SumExecutionTime(current),
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private TimeSpan SumExecutionTime(ExecutionLogRecord current)
    {
        var index = ExecutionLogRecords.IndexOf(current);
        var previousRecords = ExecutionLogRecords.Take(index).ToList();
        var timestamps = previousRecords.Select(x => x.Timestamp).ToList();
        
        var totalTime = timestamps
            .Zip(timestamps.Skip(1), (t1, t2) => t2 - t1)
            .Aggregate(TimeSpan.Zero, (acc, timeDiff) => acc + timeDiff);
        
        return totalTime;
    }

    private void OnTimeMetricButtonToggleChanged(bool value)
    {
        TimeMetricMode = value ? TimeMetricMode.Absolute : TimeMetricMode.Relative;
    }
}