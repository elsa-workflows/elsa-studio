using System.ComponentModel.DataAnnotations;

namespace Elsa.Studio.Workflows.Designer.Options;

/// <summary>
/// Represents the workflow capture options
/// </summary>
public class CaptureOptions
{
    /// <summary>
    /// Gets or sets the export format.
    /// Supported formats are JPEG, SVG and PNG.
    /// Default: PNG.
    /// </summary>
    [Required] public string? Format { get; set; } = "PNG";

    /// <summary>
    /// Gets or sets the filename.
    /// Default: Flowchart
    /// </summary>
    [Required] public string? FileName { get; set; } = "Flowchart";

    /// <summary>
    /// Gets or sets the padding around the workflow.
    /// Default: 150.
    /// </summary>
    public int Padding { get; set; } = 150;
}