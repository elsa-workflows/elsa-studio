using Elsa.Studio.Dashboard.Models;

namespace Elsa.Studio.Dashboard.Services;

public interface IDashboardService
{
    Task<DashboardLoadResult> LoadAsync(string range, bool includeSystem = false, CancellationToken cancellationToken = default);
}
