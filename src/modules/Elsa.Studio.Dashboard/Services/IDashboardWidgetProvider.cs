using Elsa.Studio.Dashboard.Models;

namespace Elsa.Studio.Dashboard.Services;

public interface IDashboardWidgetProvider
{
    IReadOnlyList<DashboardWidgetDescriptor> GetWidgets();
}
