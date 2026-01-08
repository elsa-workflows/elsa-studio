using Microsoft.JSInterop;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Starts a per-user browser-side timer that periodically POSTs to the refresh endpoint.
/// The browser request carries the authenticated cookie, enabling persisted token refresh.
/// </summary>
public class BrowserRefreshPingService(IJSRuntime jsRuntime, IOptions<OidcPersistedRefreshClientOptions> options)
{
    /// <summary>
    /// Starts the refresh ping loop.
    /// Safe to call multiple times.
    /// </summary>
    public async ValueTask StartAsync(CancellationToken cancellationToken = default)
        => await jsRuntime.InvokeVoidAsync("elsaStudioOidcRefresh.start", cancellationToken, options.Value.RefreshEndpointPath, (int)options.Value.Interval.TotalMilliseconds);
}

