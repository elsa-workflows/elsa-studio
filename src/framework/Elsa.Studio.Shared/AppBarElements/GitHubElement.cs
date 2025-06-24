using Elsa.Studio.Components.AppBar;
using Elsa.Studio.Models;

namespace Elsa.Studio.AppBarElements;

/// <summary>
/// Represents a GitHub element in the application bar.
/// </summary>
/// <remarks>
/// Displays a button linking to the project's GitHub repository, typically used for quick access to source code or related resources.
/// </remarks>
public class GitHubElement : AppBarElement<GitHub>
{
    /// <inheritdoc />
    public override float Order { get; set; } = 15;
}