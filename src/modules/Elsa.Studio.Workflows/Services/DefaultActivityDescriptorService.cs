using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.Services;

public class DefaultActivityDescriptorService : IActivityDescriptorService
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;

    public DefaultActivityDescriptorService(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }
    
    public async Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var response = await _backendConnectionProvider
            .GetApi<IActivityDescriptorsApi>()
            .ListAsync(new ListActivityDescriptorsRequest(), cancellationToken);
        
        return response.Items;
    }
}