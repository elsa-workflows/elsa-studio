namespace Elsa.Studio.Workflows.Designer.Models;

public class X6Graph
{
    public ICollection<X6Node> Nodes { get; set; } = new List<X6Node>();
    public ICollection<X6Edge> Edges { get; set; } = new List<X6Edge>();
}