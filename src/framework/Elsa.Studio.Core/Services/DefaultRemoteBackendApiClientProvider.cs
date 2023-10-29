using Elsa.Api.Client.Extensions;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides API clients to the remote backend.
/// </summary>
public class DefaultRemoteBackendApiClientProvider : IRemoteBackendApiClientProvider
{
    private readonly IRemoteBackendAccessor _remoteBackendAccessor;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultRemoteBackendApiClientProvider"/> class.
    /// </summary>
    public DefaultRemoteBackendApiClientProvider(IRemoteBackendAccessor remoteBackendAccessor, IServiceProvider serviceProvider)
    {
        _remoteBackendAccessor = remoteBackendAccessor;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public Uri Url => _remoteBackendAccessor.RemoteBackend.Url;

    /// <inheritdoc />
    public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken) where T : class
    {
        var backendUrl = _remoteBackendAccessor.RemoteBackend.Url;
        var client = _serviceProvider.CreateApi<T>(backendUrl);
        return ValueTask.FromResult(client);
    }
}