namespace Elsa.Studio.ServerLogs.Models;

/// <summary>
/// A backend process, pod, or host that emits server log events.
/// </summary>
public class ServerLogSource
{
    public string Id { get; set; } = default!;
    public string DisplayName { get; set; } = default!;
    public string? ServiceName { get; set; }
    public string? PodName { get; set; }
    public string? ContainerName { get; set; }
    public string? Namespace { get; set; }
    public string? NodeName { get; set; }
    public ServerLogSourceStatus Status { get; set; }
    public DateTimeOffset LastSeen { get; set; }
}
