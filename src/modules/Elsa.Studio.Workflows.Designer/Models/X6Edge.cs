namespace Elsa.Studio.Workflows.Designer.Models;

public class X6Edge
{
    public X6Endpoint Source { get; set; } = null!;
    public X6Endpoint Target { get; set; } = null!;
    public string? Shape { get; set; }
    public X6Position[] Vertices { get; set; } = [];
}