using System.Reflection;
using Elsa.Dashboard.Models;

namespace Elsa.Dashboard.Contracts;

public interface IMenuService
{
    /// <summary>
    /// Returns all menu items from all menu providers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of <see cref="MenuItem"/> instances.</returns>
    ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns all menu item groups from all menu group providers.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of <see cref="MenuItemGroup"/> instances.</returns>
    ValueTask<IEnumerable<MenuItemGroup>> GetMenuItemGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all assemblies that contain menu providers.
    /// </summary>
    /// <returns>A list of <see cref="Assembly"/> instances.</returns>
    IEnumerable<Assembly> GetMenuAssemblies();
}