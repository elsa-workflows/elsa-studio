using System.Text.Json;

namespace Elsa.Studio.Workflows.Designer.Models;

public class X6Node
{
    public string Id { get; set; } = default!;
    public string Shape { get; set; } = default!;
    public X6Position Position { get; set; } = default!;
    public X6Size Size { get; set; } = default!;
    public JsonElement Data { get; set; }
    //public ICollection<X6Port> Ports { get; set; } = new List<X6Port>();
    public X6Ports Ports { get; set; } = new X6Ports();
}

public class X6Ports
{
    public ICollection<X6Port> Items { get; set; } = new List<X6Port>();
}