using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Workflows.Pages.WorkflowInstances.View.Components;

public partial class ElapsedTime
{
    [Parameter] public DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;
    [Parameter] public DateTimeOffset EndTime { get; set; } = DateTimeOffset.UtcNow;
    
    private TimeSpan Elapsed => EndTime - StartTime;
}