using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Services;

public class DefaultMenuGroupProvider : IMenuGroupProvider
{
    public ValueTask<IEnumerable<MenuItemGroup>> GetMenuGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = new List<MenuItemGroup>
        {
            MenuItemGroups.General,
            MenuItemGroups.Settings
        };

        return new(groups);
    }
}