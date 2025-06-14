using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Designer.Models;

public class X6ActivityNode
{
    public string Id { get; set; } = null!;
    public string Shape { get; set; } = null!;
    public X6Position Position { get; set; } = null!;
    public X6Size Size { get; set; } = null!;
    public JsonObject Data { get; set; } = null!;
    public X6Ports Ports { get; set; } = new();
    public ActivityStats? ActivityStats { get; set; }
    
}