using Elsa.Api.Client.Extensions;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Environments.Services;

/// <summary>
/// An environment-aware backend connection provider that returns the URL to the currently selected environment, if any.
/// </summary>
public class EnvironmentBackendApiClientProvider : IBackendApiClientProvider
{
    private readonly IEnvironmentService _environmentService;
    private readonly IRemoteBackendAccessor _remoteBackendAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentBackendApiClientProvider"/> class.
    /// </summary>
    public EnvironmentBackendApiClientProvider(IEnvironmentService environmentService, IRemoteBackendAccessor remoteBackendAccessor)
    {
        _environmentService = environmentService;
        _remoteBackendAccessor = remoteBackendAccessor;
    }

    /// <inheritdoc />
    public Uri Url => _environmentService.CurrentEnvironment?.Url ?? _remoteBackendAccessor.RemoteBackend.Url;

    /// <inheritdoc />
    public async ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        var services = new ServiceCollection().AddDefaultApiClients(x =>
        {
            x.BaseAddress = Url;
            //x.ConfigureHttpClient = httpClient => httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }).BuildServiceProvider();
        return services.GetRequiredService<T>();
    }
}