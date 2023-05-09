namespace Elsa.Dashboard.Models;

/// <summary>
/// A menu item group.
/// </summary>
public record MenuItemGroup(string Name, string Text, float Order = 0f);