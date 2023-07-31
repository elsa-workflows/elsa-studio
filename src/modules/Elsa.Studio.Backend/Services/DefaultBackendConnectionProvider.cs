using System.Net.Http.Headers;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Backend.Services;

/// <summary>
/// A default implementation of <see cref="IBackendConnectionProvider"/>.
/// </summary>
public class DefaultBackendConnectionProvider : IBackendConnectionProvider
{
    private readonly IBackendAccessor _backendAccessor;
    private readonly IJwtAccessor _jwtAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBackendConnectionProvider"/> class.
    /// </summary>
    public DefaultBackendConnectionProvider(IBackendAccessor backendAccessor, IJwtAccessor jwtAccessor)
    {
        _backendAccessor = backendAccessor;
        _jwtAccessor = jwtAccessor;
    }

    /// <inheritdoc />
    public Uri Url => _backendAccessor.Backend.Url;
    
    /// <summary>
    /// Gets an API client from the backend connection provider.
    /// </summary>
    /// <typeparam name="T">The API client type.</typeparam>
    /// <returns>The API client.</returns>
    public async ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken) where T : class
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