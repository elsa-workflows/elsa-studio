using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Studio.Workflows.Contracts;

public interface IActivityDescriptorService
{
    Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken = default);
}