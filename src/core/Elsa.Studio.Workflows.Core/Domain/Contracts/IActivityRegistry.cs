using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

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
    
    /// <summary>
    /// Finds an activity descriptor by its type.
    /// </summary>
    /// <param name="activityType">The activity type.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The activity descriptor.</returns>
    Task<ActivityDescriptor?> FindAsync(string activityType, CancellationToken cancellationToken = default);
}