using Elsa.Studio.Models;

namespace Elsa.Studio;

/// <summary>
/// Represents the menu item groups.
/// </summary>
public static class MenuItemGroups
{
    /// <summary>
    /// Provides the general.
    /// </summary>
    public static readonly MenuItemGroup General = new("general", "General", 0f);
    /// <summary>
    /// Provides the settings.
    /// </summary>
    public static readonly MenuItemGroup Settings = new("security", "Settings", 1000f);
}