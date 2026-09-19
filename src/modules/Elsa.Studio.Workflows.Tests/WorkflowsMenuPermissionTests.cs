using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Menu;
using Microsoft.Extensions.Localization;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public class WorkflowsMenuPermissionTests
{
    [Fact]
    public async Task BuiltInEntries_DeclareTheirViewPermissions()
    {
        var menu = new WorkflowsMenu(new PassthroughLocalizer(), []);

        var root = Assert.Single(await menu.GetMenuItemsAsync());
        Assert.Collection(
            root.SubMenuItems,
            definitions => Assert.Equal("workflows/definitions:view", definitions.RequiredPermission),
            instances => Assert.Equal("workflows/instances:view", instances.RequiredPermission));
    }

    private sealed class PassthroughLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
