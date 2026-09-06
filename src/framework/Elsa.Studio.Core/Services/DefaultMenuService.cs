using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultMenuService : IMenuService
{
    private readonly IEnumerable<IMenuProvider> _menuProviders;
    private readonly IEnumerable<IMenuGroupProvider> _menuGroupProviders;
    private readonly ICurrentUserPermissionService? _permissionService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultMenuService"/> class.
    /// </summary>
    public DefaultMenuService(
        IEnumerable<IMenuProvider> menuProviders,
        IEnumerable<IMenuGroupProvider> menuGroupProviders,
        ICurrentUserPermissionService? permissionService = null)
    {
        _menuProviders = menuProviders;
        _menuGroupProviders = menuGroupProviders;
        _permissionService = permissionService;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menu = new List<MenuItem>();
        
        foreach (var menuProvider in _menuProviders)
        {
            var menuItems = await menuProvider.GetMenuItemsAsync(cancellationToken);
            menu.AddRange(await FilterAsync(menuItems, cancellationToken));
        }

        return menu.OrderBy(x => x.Order).ToList();
    }

    private async ValueTask<IEnumerable<MenuItem>> FilterAsync(
        IEnumerable<MenuItem> menuItems,
        CancellationToken cancellationToken)
    {
        var visibleItems = new List<MenuItem>();

        foreach (var item in menuItems)
        {
            if (item.RequiredPermission is not null)
            {
                if (_permissionService is null ||
                    !await _permissionService.HasAsync(item.RequiredPermission, cancellationToken))
                    continue;
            }

            var hadChildren = item.SubMenuItems.Count > 0;
            item.SubMenuItems = (await FilterAsync(item.SubMenuItems, cancellationToken)).ToList();

            if (hadChildren && item.SubMenuItems.Count == 0 && string.IsNullOrWhiteSpace(item.Href))
                continue;

            visibleItems.Add(item);
        }

        return visibleItems;
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<MenuItemGroup>> GetMenuItemGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = new List<MenuItemGroup>();
        
        foreach (var menuGroupProvider in _menuGroupProviders)
        {
            var menuGroups = await menuGroupProvider.GetMenuGroupsAsync(cancellationToken);
            groups.AddRange(menuGroups);
        }

        return groups.DistinctBy(x => x.Name).OrderBy(x => x.Order).ToList();
    }
}
