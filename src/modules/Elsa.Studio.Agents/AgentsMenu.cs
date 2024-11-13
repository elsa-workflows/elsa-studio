using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Elsa.Studio.Workflows.Services;
using MudBlazor;

namespace Elsa.Studio.Agents;

/// A menu provider for the Agents module.
public class AgentsMenu : IMenuProvider, IMenuGroupProvider
{
    private readonly LocalizationService _localizer;
    public AgentsMenu(LocalizationService localizer)
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
                Icon = AgentIcons.Robot,
                Href = "ai/agents",
                Text = _localizer["Agents"],
                GroupName = MenuItemGroups.General.Name
            },
            new()
            {
                Icon = AgentIcons.AI,
                Text = _localizer["Agents"],
                GroupName = MenuItemGroups.Settings.Name,
                SubMenuItems =
                [
                    new MenuItem
                    {
                        Icon = Icons.Material.Outlined.Key,
                        Href = "ai/api-keys",
                        Text = _localizer["API Keys"]
                    },
                    new MenuItem
                    {
                        Icon = Icons.Material.Outlined.MiscellaneousServices,
                        Href = "ai/services",
                        Text = _localizer["Services"]
                    }
                ]
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(menuItems);
    }

    /// <inheritdoc />
    public ValueTask<IEnumerable<MenuItemGroup>> GetMenuGroupsAsync(CancellationToken cancellationToken = default)
    {
        var groups = new List<MenuItemGroup>
        {
            //new("agents", "Intelligent Agents", 10f)
        };

        return new ValueTask<IEnumerable<MenuItemGroup>>(groups);
    }
}