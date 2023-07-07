using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Requests;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

public class RemoteActivityRegistry : IActivityRegistry
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;
    private ICollection<ActivityDescriptor>? _activityDescriptors;

    public RemoteActivityRegistry(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }

    public void Refresh()
    {
        _activityDescriptors = null;
    }

    public async Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        return _activityDescriptors ??= (await ListInternalAsync(cancellationToken)).ToList();
    }

    public async Task<ActivityDescriptor?> FindAsync(string activityType, CancellationToken cancellationToken = default)
    {
        var descriptors = await ListAsync(cancellationToken);
        return descriptors.FirstOrDefault(x => x.TypeName == activityType);
    }

    private async Task<IEnumerable<ActivityDescriptor>> ListInternalAsync(CancellationToken cancellationToken = default)
    {
        var response = await _backendConnectionProvider
            .GetApi<IActivityDescriptorsApi>()
            .ListAsync(new ListActivityDescriptorsRequest(), cancellationToken);
        
        return response.Items;
    }
}