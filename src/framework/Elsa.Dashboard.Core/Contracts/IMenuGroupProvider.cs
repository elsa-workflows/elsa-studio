using Elsa.Dashboard.Models;

namespace Elsa.Dashboard.Contracts;

/// <summary>
/// Returns menu item groups to the dashboard.
/// </summary>
public interface IMenuGroupProvider
{
    /// <summary>
    /// Returns menu item groups to the dashboard.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A collection of <see cref="MenuItemGroup"/> instances.</returns>
    ValueTask<IEnumerable<MenuItemGroup>> GetMenuGroupsAsync(CancellationToken cancellationToken = default);
}