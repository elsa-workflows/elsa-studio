using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

public interface IActivityRegistry
{
    /// <summary>
    /// Refreshes the list of activity descriptors.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Ensures that the list of activity descriptors is loaded.
    /// </summary>
    Task EnsureLoadedAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Returns a list of the latest versions of activity descriptors.
    /// </summary>
    /// <returns>A list of activity descriptors.</returns>
    IEnumerable<ActivityDescriptor> List();

    /// <summary>
    /// Finds an activity descriptor by its type.
    /// </summary>
    /// <param name="activityType">The activity type.</param>
    /// <param name="version">The activity version.</param>
    /// <returns>The activity descriptor.</returns>
    ActivityDescriptor? Find(string activityType, int? version = default);
}