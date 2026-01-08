namespace Elsa.Studio.Authentication.OpenIdConnect.Models;

/// <summary>
/// Configuration options for OpenID Connect authentication.
/// </summary>
public class OidcOptions
{
    /// <summary>
    /// Gets or sets the authority URL of the OpenID Connect provider.
    /// </summary>
    public string Authority { get; set; } = default!;

    /// <summary>
    /// Gets or sets the client ID registered with the OpenID Connect provider.
    /// </summary>
    public string ClientId { get; set; } = default!;

    /// <summary>
    /// Gets or sets the client secret (optional, typically not used with public clients like WASM).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Gets or sets the response type for the authentication request.
    /// </summary>
    public string ResponseType { get; set; } = "code";

    /// <summary>
    /// Gets or sets the scopes to request.
    /// </summary>
    public string[] Scopes { get; set; } = ["openid", "profile", "offline_access"];

    /// <summary>
    /// Gets or sets whether to use PKCE (Proof Key for Code Exchange).
    /// </summary>
    public bool UsePkce { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to save tokens in the authentication properties (Server only).
    /// </summary>
    public bool SaveTokens { get; set; } = true;

    /// <summary>
    /// Gets or sets the callback path for handling the authentication response (Server only).
    /// </summary>
    public string CallbackPath { get; set; } = "/signin-oidc";

    /// <summary>
    /// Gets or sets the sign-out callback path (Server only).
    /// </summary>
    public string SignedOutCallbackPath { get; set; } = "/signout-callback-oidc";

    /// <summary>
    /// Gets or sets whether to require HTTPS metadata.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to get claims from the user info endpoint.
    /// </summary>
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = true;

    /// <summary>
    /// Gets or sets the metadata address (optional, auto-discovered from Authority if not set).
    /// </summary>
    public string? MetadataAddress { get; set; }
}
