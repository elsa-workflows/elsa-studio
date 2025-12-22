namespace Elsa.Studio.Branding.Models;

/// <summary>
/// Represents the default toolbar link display options.
/// </summary>
public class DefaultAppBarIcons
{
    /// <summary>
    /// Gets or sets a value indicating whether a link to documentation should be displayed in the user interface.
    /// </summary>
    public bool ShowDocumentationLink { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the GitHub link is displayed in the user interface.
    /// </summary>
    public bool ShowGitHubLink { get; set; }
}