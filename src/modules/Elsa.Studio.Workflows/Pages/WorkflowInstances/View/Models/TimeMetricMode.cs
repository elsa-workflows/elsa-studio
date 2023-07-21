namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Models;

public enum TimeMetricMode
{
    /// <summary>
    /// Show the delta between the current and previous log entry.
    /// </summary>
    Relative,
    
    /// <summary>
    /// Show the accumulated time since the workflow instance started until the current log entry.
    /// </summary>
    Accumulated
}