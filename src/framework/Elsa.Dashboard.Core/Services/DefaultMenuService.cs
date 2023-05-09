using System.Reflection;
using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Models;

namespace Elsa.Dashboard.Services;

public class DefaultMenuService : IMenuService
{
    private readonly IEnumerable<IMenuProvider> _menuProviders;
    private readonly IEnumerable<IMenuGroupProvider> _menuGroupProviders;

    public DefaultMenuService(IEnumerable<IMenuProvider> menuProviders, IEnumerable<IMenuGroupProvider> menuGroupProviders)
    {
        _menuProviders = menuProviders;
        _menuGroupProviders = menuGroupProviders;
    }
    
    public async ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menu = new List<MenuItem>();
        
        foreach (var menuProvider in _menuProviders)
        {
            var menuItems = await menuProvider.GetMenuItemsAsync(cancellationToken);
            menu.AddRange(menuItems);
        }

        return menu.OrderByDescending(x => x.Order).ToList();
    }

    public async ValueTask<IEnumerable<MenuItemGroup>> GetMenuItemGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = new List<MenuItemGroup>();
        
        foreach (var menuGroupProvider in _menuGroupProviders)
        {
            var menuGroups = await menuGroupProvider.GetMenuGroupsAsync(cancellationToken);
            groups.AddRange(menuGroups);
        }

        return groups.DistinctBy(x => x.Name).OrderByDescending(x => x.Order).ToList();
    }

    public IEnumerable<Assembly> GetMenuAssemblies() => _menuProviders.Select(x => x.GetType().Assembly).Distinct().ToList();
}