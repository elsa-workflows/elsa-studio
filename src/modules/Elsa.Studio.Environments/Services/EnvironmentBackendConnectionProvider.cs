using System.Net.Http.Headers;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Environments.Services;

/// <summary>
/// An environment-aware backend connection provider that returns the URL to the currently selected environment, if any.
/// </summary>
public class EnvironmentBackendConnectionProvider : IBackendConnectionProvider
{
    private readonly IEnvironmentService _environmentService;
    private readonly IBackendAccessor _backendAccessor;
    private readonly IJwtAccessor _jwtAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnvironmentBackendConnectionProvider"/> class.
    /// </summary>
    public EnvironmentBackendConnectionProvider(IEnvironmentService environmentService, IBackendAccessor backendAccessor, IJwtAccessor jwtAccessor)
    {
        _environmentService = environmentService;
        _backendAccessor = backendAccessor;
        _jwtAccessor = jwtAccessor;
    }

    /// <inheritdoc />
    public Uri Url => _environmentService.CurrentEnvironment?.Url ?? _backendAccessor.Backend.Url;

    /// <inheritdoc />
    public async ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        var accessToken = await _jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);

        var services = new ServiceCollection().AddElsaClient(x =>
        {
            x.BaseAddress = Url;
            x.ConfigureHttpClient = httpClient => httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }).BuildServiceProvider();
        return services.GetRequiredService<T>();
    }
}