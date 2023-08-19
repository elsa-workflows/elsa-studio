using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

public interface IActivityRegistryProvider
{
    /// <summary>
    /// Returns a list of activity descriptors.
    /// </summary>
    Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}