namespace Elsa.Studio.Workflows.Designer.Models;

public class X6Port
{
    public string Id { get; set; } = default!;
    public string Group { get; set; } = default!;
    public string? Type { get; set; }
    public string? Position { get; set; }
    public IDictionary<string, object>? Attrs { get; set; }
}