namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Client-side options for invoking the persisted refresh endpoint.
/// </summary>
public class OidcPersistedRefreshClientOptions
{
    /// <summary>
    /// The relative URL of the refresh endpoint.
    /// </summary>
    public string RefreshEndpointPath { get; set; } = "/authentication/refresh";

    /// <summary>
    /// How often the client should ping the refresh endpoint.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(1);
}

