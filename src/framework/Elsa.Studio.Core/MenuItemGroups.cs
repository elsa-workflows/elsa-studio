using Elsa.Studio.Models;

namespace Elsa.Studio;

/// <summary>
/// Provides predefined menu item groups for organizing application navigation.
/// </summary>
public static class MenuItemGroups
{
    /// <summary>
    /// Gets the general menu item group for common application features.
    /// </summary>
    public static readonly MenuItemGroup General = new("general", "General", 0f);

    /// <summary>
    /// Gets the settings menu item group for configuration and administrative features.
    /// </summary>
    public static readonly MenuItemGroup Settings = new("security", "Settings", 1000f);
}