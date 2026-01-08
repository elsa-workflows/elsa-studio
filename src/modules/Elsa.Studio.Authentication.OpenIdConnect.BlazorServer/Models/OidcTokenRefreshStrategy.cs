namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;

/// <summary>
/// Determines how Blazor Server should perform access-token refresh.
/// </summary>
public enum OidcTokenRefreshStrategy
{
    /// <summary>
    /// Best-effort refresh. The module will only refresh when it can also renew the auth cookie
    /// (i.e., when HTTP response headers can still be written).
    /// </summary>
    BestEffort = 0,

    /// <summary>
    /// Persist tokens by renewing the auth cookie via a dedicated refresh endpoint.
    /// This can maintain long-lived sessions without interactive re-authentication.
    /// </summary>
    Persisted = 1
}

