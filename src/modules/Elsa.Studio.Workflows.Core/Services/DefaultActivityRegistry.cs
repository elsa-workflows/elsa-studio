using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Workflows.Core.Contracts;

namespace Elsa.Studio.Workflows.Core.Services;

public class DefaultActivityRegistry : IActivityRegistry
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;
    private ICollection<ActivityDescriptor>? _activityDescriptors;

    public DefaultActivityRegistry(IBackendConnectionProvider backendConnectionProvider)
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
    
    private async Task<IEnumerable<ActivityDescriptor>> ListInternalAsync(CancellationToken cancellationToken = default)
    {
        var response = await _backendConnectionProvider
            .GetApi<IActivityDescriptorsApi>()
            .ListAsync(new ListActivityDescriptorsRequest(), cancellationToken);
        
        return response.Items;
    }
}