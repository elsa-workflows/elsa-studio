using Elsa.Dashboard.Contracts;
using Elsa.Dashboard.Models;

namespace Elsa.Dashboard.Services;

public class DefaultMenuGroupProvider : IMenuGroupProvider
{
    public ValueTask<IEnumerable<MenuItemGroup>> GetMenuGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = new List<MenuItemGroup>
        {
            MenuItemGroups.General
        };

        return new(groups);
    }
}