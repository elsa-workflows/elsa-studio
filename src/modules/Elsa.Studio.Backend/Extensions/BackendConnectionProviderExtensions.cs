using Elsa.Api.Client.Extensions;
using Elsa.Studio.Backend.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Backend.Extensions;

public static class BackendConnectionProviderExtensions
{
    public static T GetApi<T>(this IBackendConnectionProvider backendConnectionProvider) where T : class
    {
        var serverUrl = backendConnectionProvider.Url;
        var services = new ServiceCollection().AddElsaClient(x => x.BaseAddress = serverUrl).BuildServiceProvider();
        return services.GetRequiredService<T>();
    }
}