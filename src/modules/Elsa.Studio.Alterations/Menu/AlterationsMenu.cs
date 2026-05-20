using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Services;
using Microsoft.AspNetCore.Components.Routing;
using MudBlazor;

namespace Elsa.Studio.Alterations.Menu;

/// <summary>
/// Exposes the top-level "Alterations" menu group with sub-entries to plans/instances.
/// </summary>
public class AlterationsMenu(ILocalizer localizer) : IMenuProvider
{
    /// <inheritdoc />
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<MenuItem>
        {
            new()
            {
                Icon = Icons.Material.Outlined.Tune,
                Text = localizer["Alterations"],
                GroupName = MenuItemGroups.General.Name,
                SubMenuItems =
                {
                    new MenuItem
                    {
                        Text = localizer["Instances"],
                        Href = "alterations/instances",
                        Icon = Icons.Material.Outlined.PlayCircleOutline,
                    },
                    // Match=All so this entry doesn't get highlighted whenever any /alterations/*
                    // route is active (otherwise both sub-items light up).
                    new MenuItem
                    {
                        Text = localizer["Plans"],
                        Href = "alterations",
                        Icon = Icons.Material.Outlined.History,
                        Match = NavLinkMatch.All,
                    }
                }
            }
        };

        return new ValueTask<IEnumerable<MenuItem>>(items);
    }
}
