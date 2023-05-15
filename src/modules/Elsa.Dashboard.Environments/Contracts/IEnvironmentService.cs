using Elsa.Dashboard.Environments.Models;

namespace Elsa.Dashboard.Environments.Contracts;

/// <summary>
/// Manages the environment in which the dashboard is running.
/// </summary>
public interface IEnvironmentService
{
    /// <summary>
    /// Returns a list of environments.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of environments.</returns>
    ValueTask<IEnumerable<WorkflowsEnvironment>> ListEnvironmentsAsync(CancellationToken cancellationToken = default);
}