using Elsa.Studio.Dashboard.Models;

namespace Elsa.Studio.Dashboard.Services;

public class DashboardWidgetProvider(
    IEnumerable<DashboardWidgetDescriptor> descriptors,
    IDashboardWidgetRegistry registry) : IDashboardWidgetProvider
{
    public IReadOnlyList<DashboardWidgetDescriptor> GetWidgets()
    {
        return descriptors
            .Concat(registry.List())
            .Select(DashboardWidgetValidator.Validate)
            .GroupBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                if (group.Count() > 1)
                    throw new InvalidOperationException($"Dashboard widget descriptor '{group.Key}' is registered more than once.");

                return group.Single();
            })
            .OrderBy(x => x.Zone)
            .ThenBy(x => x.Order)
            .ThenBy(x => x.Id, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
