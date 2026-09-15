using Elsa.Studio.Diagnostics.StructuredLogs.Menu;
using Xunit;

namespace Elsa.Studio.Diagnostics.StructuredLogs.Tests;

public class StructuredLogsMenuPermissionTests
{
    [Fact]
    public async Task StructuredLogsEntry_DeclaresItsViewPermission()
    {
        var item = Assert.Single(await new StructuredLogsMenu().GetMenuItemsAsync());

        Assert.Equal("diagnostics/structured-logs:view", item.RequiredPermission);
    }
}
