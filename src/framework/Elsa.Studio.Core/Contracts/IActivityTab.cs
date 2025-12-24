using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Represents a contract for defining activity tabs within the application.
/// </summary>
public interface IActivityTab
{
    /// <summary>
    /// Gets the title of the activity tab. This property represents a user-facing label or name
    /// for the tab within the user interface, displayed as the tab header text.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the order of the tab in relation to other tabs.
    /// This property determines the relative position of the tab when displayed, allowing tabs to
    /// be sorted or organized based on their numerical order value.
    /// </summary>
    double Order { get; }

    /// <summary>
    /// Defines a rendering function for an activity tab. This function takes a dictionary of attributes
    /// as its input and returns a <see cref="RenderFragment"/> that describes the content to be rendered
    /// within the tab.
    /// </summary>
    Func<IDictionary<string, object?>, RenderFragment> Render { get; }
}
