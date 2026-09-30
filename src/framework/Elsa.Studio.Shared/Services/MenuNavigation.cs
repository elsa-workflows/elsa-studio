using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Services;

/// <summary>
/// The navigation menu in display order: by group, then by item within the group. Shared by everything that needs to
/// agree with what the navigation shows.
/// </summary>
public static class MenuNavigation
{
    /// <summary>Loads the menu and arranges it as the navigation shows it. Groups without items are omitted.</summary>
    public static async Task<IReadOnlyList<(MenuItemGroup Group, IReadOnlyList<MenuItem> Items)>> GetNavigationAsync(this IMenuService menuService, CancellationToken cancellationToken = default)
    {
        var groups = (await menuService.GetMenuItemGroupsAsync(cancellationToken)).ToDictionary(x => x.Name);
        var items = (await menuService.GetMenuItemsAsync(cancellationToken)).ToList();

        return groups.Values
            .Select(group => (Group: group, Items: (IReadOnlyList<MenuItem>)items.Where(item => item.GroupName == group.Name).ToList()))
            .Where(entry => entry.Items.Count > 0)
            .ToList();
    }

    /// <summary>
    /// Finds the first page the navigation leads to: the first leaf in display order that has an app-relative href.
    /// </summary>
    public static string? FindFirstHref(this IEnumerable<(MenuItemGroup Group, IReadOnlyList<MenuItem> Items)> navigation) => navigation
        .SelectMany(entry => entry.Items)
        .Select(FindFirstHref)
        .FirstOrDefault(href => href != null);

    private static string? FindFirstHref(MenuItem item) => item.SubMenuItems.Count > 0
        ? item.SubMenuItems.Select(FindFirstHref).FirstOrDefault(href => href != null)
        : IsNavigable(item.Href) ? item.Href : null;

    // Only app-relative targets with a path: not blank, not the root itself, not an external or absolute URL.
    private static bool IsNavigable(string? href) =>
        !string.IsNullOrWhiteSpace(href) && href.Trim('/').Length > 0 && !Uri.TryCreate(href, UriKind.Absolute, out _) && !href.StartsWith("//");
}
