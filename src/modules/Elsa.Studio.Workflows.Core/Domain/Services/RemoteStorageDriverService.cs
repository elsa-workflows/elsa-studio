using Elsa.Api.Client.Resources.StorageDrivers.Contracts;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A storage driver service that uses a remote backend to retrieve storage drivers.
/// </summary>
public class RemoteStorageDriverService : IStorageDriverService
{
    private readonly IRemoteBackendApiClientProvider _remoteBackendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteStorageDriverService"/> class.
    /// </summary>
    public RemoteStorageDriverService(IRemoteBackendApiClientProvider remoteBackendApiClientProvider)
    {
        _remoteBackendApiClientProvider = remoteBackendApiClientProvider;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<StorageDriverDescriptor>> GetStorageDriversAsync(CancellationToken cancellationToken = default)
    {
        var api = await _remoteBackendApiClientProvider.GetApiAsync<IStorageDriversApi>(cancellationToken);
        var response = await api.ListAsync(cancellationToken);
        
        return response.Items;
    }
}