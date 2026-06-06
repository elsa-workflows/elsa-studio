using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Xunit;

namespace Elsa.Studio.Dashboard.Tests;

public class DashboardWidgetProviderTests
{
    private readonly DashboardWidgetRegistry _registry = new();

    [Fact]
    public void GetWidgets_OrdersByZoneOrderAndId()
    {
        _registry.Add(Descriptor("secondary.b", DashboardWidgetZone.Secondary, 20));
        _registry.Add(Descriptor("primary.a", DashboardWidgetZone.Primary, 20));
        _registry.Add(Descriptor("primary.b", DashboardWidgetZone.Primary, 10));

        var provider = new DashboardWidgetProvider([], _registry);

        var results = provider.GetWidgets();

        Assert.Equal(["primary.b", "primary.a", "secondary.b"], results.Select(x => x.Id));
    }

    [Fact]
    public void Add_WhenDuplicateId_Throws()
    {
        _registry.Add(Descriptor("duplicate", DashboardWidgetZone.Primary, 0));

        var exception = Assert.Throws<InvalidOperationException>(() => _registry.Add(Descriptor("DUPLICATE", DashboardWidgetZone.Primary, 1)));

        Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetWidgets_WhenStaticAndRegistryDuplicateId_Throws()
    {
        _registry.Add(Descriptor("duplicate", DashboardWidgetZone.Primary, 0));
        var provider = new DashboardWidgetProvider([Descriptor("duplicate", DashboardWidgetZone.Secondary, 0)], _registry);

        var exception = Assert.Throws<InvalidOperationException>(() => provider.GetWidgets());

        Assert.Contains("duplicate", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static DashboardWidgetDescriptor Descriptor(string id, DashboardWidgetZone zone, int order) => new()
    {
        Id = id,
        ComponentType = typeof(object),
        Zone = zone,
        Order = order
    };
}
