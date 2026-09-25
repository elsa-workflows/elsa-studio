using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Microsoft.Extensions.Logging;
using Refit;

namespace Elsa.Studio.Environments.Services;

/// <summary>
/// Loads available environments from the current backend into <see cref="IEnvironmentService"/>.
/// </summary>
/// <remarks>
/// This is circuit-scoped work. Do not register it as <c>IStartupTask</c>: on Blazor Server
/// those tasks run from <c>RunStartupTasksHostedService</c> during <c>Host.StartAsync</c>,
/// before JS interop exists for ElsaIdentity token storage.
/// </remarks>
public class EnvironmentLoader(
    IBackendApiClientProvider backendApiClientProvider,
    IEnvironmentService environmentService,
    ILogger<EnvironmentLoader> logger)
{
    private Task? _loadTask;

    /// <summary>
    /// Fetches environments once for this circuit and stores them.
    /// Load failures are logged and leave the picker empty.
    /// </summary>
    public Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        return _loadTask ??= LoadAsync(cancellationToken);
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        try
        {
            var environmentsClient = await backendApiClientProvider.GetApiAsync<IEnvironmentsClient>(cancellationToken);
            var response = await environmentsClient.ListEnvironmentsAsync(cancellationToken);
            environmentService.SetEnvironments(response.Environments, response.DefaultEnvironmentName);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Could not reach the environments API; the picker will stay empty.");
        }
        catch (ApiException exception) when (exception.StatusCode is HttpStatusCode.NotFound
            or HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden)
        {
            logger.LogWarning("Environments API returned {StatusCode}; the picker will stay empty.", (int)exception.StatusCode);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // No token yet, JS interop unavailable, or any other load failure must not crash the circuit.
            logger.LogWarning(exception, "Could not load environments; the picker will stay empty.");
        }
    }
}
