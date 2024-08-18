using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using MudBlazor;

namespace Elsa.Studio.Agents;

/// A menu provider for the Agents module.
public class AgentsMenu : IMenuProvider, IMenuGroupProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var menuItems = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Filled.Add,
                Href = "ai/agents",
                Text = "Agents",
                GroupName = MenuItemGroups.General.Name
            },
            new()
            {
                Icon = Icons.Material.Filled.Add,
                Text = "AI",
                GroupName = MenuItemGroups.Settings.Name,
                SubMenuItems =
                [
                    new MenuItem
                    {
                        Icon = Icons.Material.Filled.Add,
                        Href = "ai/api-keys",
                        Text = "API Keys"
                    },
                    new MenuItem
                    {
                        Icon = Icons.Material.Filled.Add,
                        Href = "ai/services",
                        Text = "Services"
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