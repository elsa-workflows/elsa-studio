using Elsa.Api.Client.Resources.IncidentStrategies.Contracts;
using Elsa.Api.Client.Resources.IncidentStrategies.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// Provides incident strategies from a remote server.
/// </summary>
public class RemoteIncidentStrategiesProvider : IIncidentStrategiesProvider
{
    private readonly IRemoteBackendApiClientProvider _remoteBackendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteIncidentStrategiesProvider"/> class.
    /// </summary>
    public RemoteIncidentStrategiesProvider(IRemoteBackendApiClientProvider remoteBackendApiClientProvider)
    {
        _remoteBackendApiClientProvider = remoteBackendApiClientProvider;
    }
    
    /// <inheritdoc />
    public async ValueTask<IEnumerable<IncidentStrategyDescriptor>> GetIncidentStrategiesAsync(CancellationToken cancellationToken = default)
    {
        var api = await _remoteBackendApiClientProvider.GetApiAsync<IIncidentStrategiesApi>(cancellationToken);
        var response = await api.ListAsync(cancellationToken);

        return response.Items;
    }
}