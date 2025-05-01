using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using MudBlazor;

namespace Elsa.Studio.Labels.Menu;

/// <summary>
/// Provides the menu items for the Labels module.
/// </summary>
public class LabelsMenu(ILocalizer localizer) : IMenuProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Label,
                Href = "Labels",
                Text = localizer["Labels"],
                GroupName = MenuItemGroups.Settings.Name
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}