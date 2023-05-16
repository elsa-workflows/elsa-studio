using Elsa.Studio.Environments.Models;
using Refit;

namespace Elsa.Studio.Environments.Contracts;

/// <summary>
/// An HTTP client for the primary server.
/// </summary>
public interface IPrimaryServerClient
{
    /// <summary>
    /// Returns a list of environments from the primary server.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of environments.</returns>
    [Get("/workflows-environments")]
    Task<IEnumerable<WorkflowsEnvironment>> ListEnvironmentsAsync(CancellationToken cancellationToken = default);
}