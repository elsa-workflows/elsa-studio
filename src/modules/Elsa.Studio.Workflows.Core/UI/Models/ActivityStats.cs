namespace Elsa.Studio.Workflows.UI.Models;

public class ActivityStats
{
    public long Started { get; set; }
    public long Completed { get; set; }
    public long Uncompleted { get; set; }
    public bool Blocked { get; set; }
    public bool Faulted { get; set; }
}