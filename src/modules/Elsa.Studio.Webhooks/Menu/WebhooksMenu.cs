using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Services;
using MudBlazor;

namespace Elsa.Studio.Webhooks.Menu;

public class WebhooksMenu : IMenuProvider
{
    private readonly LocalizationService _localizer;
    public WebhooksMenu(LocalizationService localizer)
    {
        _localizer = localizer;
    }
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Http,
                Href = "webhooks",
                Text = _localizer["Webhooks"],
                GroupName = MenuItemGroups.Settings.Name
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }
}