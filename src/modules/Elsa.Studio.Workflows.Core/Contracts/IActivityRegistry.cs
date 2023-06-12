using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Studio.Workflows.Core.Contracts;

public interface IActivityRegistry
{
    /// <summary>
    /// Refreshes the list of activity descriptors.
    /// </summary>
    void Refresh();
    
    /// <summary>
    /// Returns a list of activity descriptors.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A list of activity descriptors.</returns>
    Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}