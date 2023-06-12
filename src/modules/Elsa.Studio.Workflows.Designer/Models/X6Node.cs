namespace Elsa.Studio.Workflows.Designer.Models;

public class X6Node
{
    public string Id { get; set; } = default!;
    public string Shape { get; set; } = default!;
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public object? Data { get; set; }
    public ICollection<X6Port> Ports { get; set; } = new List<X6Port>();
}