using System.Text.Json.Serialization;

namespace Elsa.Studio.Workflows.Designer.Models;

public class X6Graph
{
    [JsonConstructor]
    public X6Graph()
    {
    }
    
    public X6Graph(IEnumerable<X6ActivityNode> nodes, IEnumerable<X6Edge> edges)
    {
        Nodes = nodes.ToList();
        Edges = edges.ToList();
    }

    public ICollection<X6ActivityNode> Nodes { get; set; } = new List<X6ActivityNode>();
    public ICollection<X6Edge> Edges { get; set; } = new List<X6Edge>();
}