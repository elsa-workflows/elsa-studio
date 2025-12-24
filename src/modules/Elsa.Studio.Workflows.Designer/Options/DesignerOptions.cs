namespace Elsa.Studio.Workflows.Designer.Options;

/// <summary>
/// Represents configuration options for designer.
/// </summary>
public class DesignerOptions
{
    /// <summary>
    /// Gets or sets the designer css class.
    /// </summary>
    public string? DesignerCssClass { get; set; } = "elsa-flowchart-diagram-designer-v2";
    /// <summary>
    /// Gets or sets the graph settings.
    /// </summary>
    public X6GraphSettings GraphSettings { get; set; } = new();
}