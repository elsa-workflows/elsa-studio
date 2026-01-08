namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;

/// <summary>
/// Options for server-side OpenID Connect access-token refresh.
/// </summary>
public class OidcTokenRefreshOptions
{
    /// <summary>
    /// Enables silent refresh using the refresh token stored in the auth cookie (requires <c>SaveTokens=true</c>
    /// and requesting <c>offline_access</c> so a refresh token is issued).
    /// </summary>
    public bool EnableRefreshTokens { get; set; } = true;

    /// <summary>
    /// How long before expiry we attempt refresh.
    /// </summary>
    public TimeSpan RefreshSkew { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Optional override for the token endpoint. If not set, it will be discovered via OIDC metadata.
    /// </summary>
    public string? TokenEndpoint { get; set; }

    /// <summary>
    /// Optional override for the client ID. If not set, uses the configured OIDC client id.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Optional override for the client secret.
    /// </summary>
    public string? ClientSecret { get; set; }
}

