using System.Text.Json.Nodes;

namespace Elsa.Studio.Workflows.Models;

public class WorkflowInstanceObserverContext
{
    public string WorkflowInstanceId { get; set; } = default!;
    public JsonObject? ContainerActivity { get; set; }
    public CancellationToken CancellationToken { get; set; }
}