namespace Elsa.Studio.Workflows.Designer.Options;

public class DesignerOptions
{
    public string? DesignerCssClass { get; set; } = "elsa-flowchart-diagram-designer-v2";
    public X6GraphSettings GraphSettings { get; set; } = new();
}