using Elsa.Api.Client.Resources.StorageDrivers.Contracts;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.Services;

public class RemoteStorageDriverService : IStorageDriverService
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;

    public RemoteStorageDriverService(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }
    
    public async Task<IEnumerable<StorageDriverDescriptor>> GetStorageDriversAsync(CancellationToken cancellationToken = default)
    {
        var response = await _backendConnectionProvider
            .GetApi<IStorageDriversApi>()
            .ListAsync(cancellationToken);
        
        return response.Items;
    }
}