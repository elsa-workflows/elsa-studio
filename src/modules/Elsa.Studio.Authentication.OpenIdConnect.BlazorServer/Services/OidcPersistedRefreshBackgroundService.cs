using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Periodically calls the persisted refresh endpoint so cookies can be renewed on a normal HTTP request.
/// NOTE: This service does not have access to per-user authentication cookies and therefore cannot reliably
/// trigger persisted refresh for signed-in users. Prefer invoking the refresh endpoint from the browser (per-user)
/// or from a request that carries the user's auth cookie.
/// </summary>
public class OidcPersistedRefreshBackgroundService(
    IHttpClientFactory httpClientFactory,
    IOptions<OidcPersistedRefreshClientOptions> options,
    ILogger<OidcPersistedRefreshBackgroundService> logger,
    IOptions<OidcTokenRefreshOptions> refreshOptions) : BackgroundService
{
    internal const string ClientName = "Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.PersistedRefresh";

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // This background service runs process-wide and cannot carry user cookies.
        // To avoid giving a false sense of security, we only run when the strategy is BestEffort.
        if (refreshOptions.Value.Strategy == OidcTokenRefreshStrategy.Persisted)
        {
            logger.LogWarning("OidcPersistedRefreshBackgroundService is not effective for Persisted refresh because it cannot send per-user auth cookies. Use a browser-side ping to POST {Path} instead.", options.Value.RefreshEndpointPath);
            return;
        }

        var settings = options.Value;

        // Delay loop.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = httpClientFactory.CreateClient(ClientName);

                // POST because it potentially mutates auth cookie.
                using var request = new HttpRequestMessage(HttpMethod.Post, settings.RefreshEndpointPath);

                var response = await client.SendAsync(request, stoppingToken);

                // Ignore failures; the next interactive request will handle auth.
                if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NoContent)
                    logger.LogDebug("Persisted OIDC refresh ping returned status code {StatusCode}", response.StatusCode);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Shutting down.
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Persisted OIDC refresh ping failed");
            }

            await Task.Delay(settings.Interval, stoppingToken);
        }
    }
}
