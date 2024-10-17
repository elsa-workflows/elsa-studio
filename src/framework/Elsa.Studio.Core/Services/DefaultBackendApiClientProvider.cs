using System.Reflection;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides API clients to the remote backend.
/// </summary>
public class DefaultBackendApiClientProvider(IRemoteBackendAccessor remoteBackendAccessor, IBlazorServiceAccessor blazorServiceAccessor, IServiceProvider serviceProvider) : IBackendApiClientProvider
{
    /// <inheritdoc />
    public Uri Url => remoteBackendAccessor.RemoteBackend.Url;

    /// <inheritdoc />
    public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken) where T : class
    {
        var backendUrl = remoteBackendAccessor.RemoteBackend.Url;
        var client = serviceProvider.CreateApi<T>(backendUrl);
        var decorator = DispatchProxy.Create<T, BlazorScopedProxyApi<T>>();
        (decorator as BlazorScopedProxyApi<T>)!.Initialize(client, blazorServiceAccessor, serviceProvider);
        return new(decorator);
    }
}