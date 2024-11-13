using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Services;
using MudBlazor;

namespace Elsa.Studio.Secrets.Menu;

/// <summary>
/// Provides the menu items for the Secrets module.
/// </summary>
public class SecretsMenu : IMenuProvider
{
    private readonly ElsaLocalization _localizer;
    public SecretsMenu(ElsaLocalization localizer)
    {
        _localizer = localizer;
    }
    /// <inheritdoc />
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Key,
                Href = "secrets",
                Text = _localizer["Secrets"],
                GroupName = MenuItemGroups.Settings.Name
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}