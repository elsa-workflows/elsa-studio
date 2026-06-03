using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;

namespace Elsa.Studio.Workflows.Dashboard;

public class WorkflowDashboardDataProvider(IDashboardService dashboardService) : IWorkflowDashboardDataProvider
{
    private DashboardLoadRequest? _currentRequest;

    public Task<DashboardLoadResult> LoadAsync(DashboardWidgetContext context, CancellationToken cancellationToken = default)
    {
        if (_currentRequest?.Matches(context) == true)
            return _currentRequest.Task.WaitAsync(cancellationToken);

        var task = dashboardService.LoadAsync(context.Range, cancellationToken: cancellationToken);
        _currentRequest = new DashboardLoadRequest(context.Range, context.RefreshVersion, task);
        return task;
    }

    private record DashboardLoadRequest(string Range, int RefreshVersion, Task<DashboardLoadResult> Task)
    {
        public bool Matches(DashboardWidgetContext context) => Range == context.Range && RefreshVersion == context.RefreshVersion;
    }
}
