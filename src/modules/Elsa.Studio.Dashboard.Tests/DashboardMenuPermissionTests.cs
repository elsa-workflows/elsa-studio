using Elsa.Studio.Dashboard.Menu;
using Elsa.Studio.Localization;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public class DashboardMenuPermissionTests
{
    [Fact]
    public async Task DashboardEntry_DeclaresItsViewPermission()
    {
        var menu = new DashboardMenu(new PassthroughLocalizer());

        var item = Assert.Single(await menu.GetMenuItemsAsync());
        Assert.Equal("dashboard:view", item.RequiredPermission);
    }

    private sealed class PassthroughLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
