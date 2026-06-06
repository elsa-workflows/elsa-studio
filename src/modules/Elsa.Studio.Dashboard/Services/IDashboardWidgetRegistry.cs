using Elsa.Studio.Dashboard.Models;

namespace Elsa.Studio.Dashboard.Services;

public interface IDashboardWidgetRegistry
{
    void Add(DashboardWidgetDescriptor descriptor);
    IReadOnlyCollection<DashboardWidgetDescriptor> List();
}
