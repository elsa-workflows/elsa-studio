using Elsa.Api.Client.Extensions;
using Elsa.Studio.Backend.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Backend.Extensions;

/// <summary>
/// Provides extension methods for <see cref="IBackendConnectionProvider"/>.
/// </summary>
public static class BackendConnectionProviderExtensions
{
    // /// <summary>
    // /// Gets an API client from the backend connection provider.
    // /// </summary>
    // /// <param name="backendConnectionProvider">The backend connection provider.</param>
    // /// <typeparam name="T">The API client type.</typeparam>
    // /// <returns>The API client.</returns>
    // public static T GetApi<T>(this IBackendConnectionProvider backendConnectionProvider) where T : class
    // {
    //     var serverUrl = backendConnectionProvider.Url;
    //     var services = new ServiceCollection().AddElsaClient(x =>
    //     {
    //         x.BaseAddress = serverUrl;
    //         x.ConfigureHttpClient = httpClient
    //     }).BuildServiceProvider();
    //     return services.GetRequiredService<T>();
    // }
}