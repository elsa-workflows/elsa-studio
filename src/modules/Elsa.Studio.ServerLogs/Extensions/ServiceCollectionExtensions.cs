using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.ServerLogs.Client;
using Elsa.Studio.ServerLogs.Contracts;
using Elsa.Studio.ServerLogs.Menu;
using Elsa.Studio.ServerLogs.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.ServerLogs.Extensions;

/// <summary>
/// Contains extension methods for the server logs module.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the server logs module.
    /// </summary>
    public static IServiceCollection AddServerLogsModule(this IServiceCollection services, BackendApiConfig backendApiConfig)
    {
        return services
            .AddScoped<IFeature, Feature>()
            .AddScoped<IMenuProvider, ServerLogsMenu>()
            .AddScoped<IServerLogService, RemoteServerLogService>()
            .AddScoped<IServerLogObserver, SignalRServerLogObserver>()
            .AddRemoteApi<IServerLogsApi>(backendApiConfig);
    }
}
