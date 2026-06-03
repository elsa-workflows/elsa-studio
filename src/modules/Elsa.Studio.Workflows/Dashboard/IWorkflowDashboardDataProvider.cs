using Elsa.Studio.Dashboard.Models;

namespace Elsa.Studio.Workflows.Dashboard;

public interface IWorkflowDashboardDataProvider
{
    Task<DashboardLoadResult> LoadAsync(DashboardWidgetContext context, CancellationToken cancellationToken = default);
}
