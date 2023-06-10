using Elsa.Studio.Backend.Contracts;
using Refit;

namespace Elsa.Studio.Backend.Extensions;

public static class BackendConnectionProviderExtensions
{
    public static T GetApi<T>(this IBackendConnectionProvider backendConnectionProvider) where T : class
    {
        var serverUrl = backendConnectionProvider.Url.ToString();
        return RestService.For<T>(serverUrl);
    }
}