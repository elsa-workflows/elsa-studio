using Elsa.Studio.Authentication.Abstractions.Models;

namespace Elsa.Studio.Authentication.OpenIdConnect.Models;

/// <summary>
/// Configuration options for OpenID Connect authentication.
/// </summary>
public class OidcOptions : AuthenticationOptions
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
    /// Gets or sets whether to use PKCE (Proof Key for Code Exchange).
    /// </summary>
    public bool UsePkce { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to save tokens in the authentication properties (Server only).
    /// </summary>
    public bool SaveTokens { get; set; } = true;

    /// <summary>
    /// Gets or sets the callback path for handling the authentication response.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Blazor Server typically uses <c>/signin-oidc</c>.</description></item>
    /// <item><description>Blazor WebAssembly uses <c>/authentication/login-callback</c>.</description></item>
    /// </list>
    /// When using the Blazor WebAssembly authentication stack, the identity provider expects an absolute <c>redirect_uri</c>.
    /// The framework will convert these paths into absolute URIs based on the current base URI.
    /// </remarks>
    public string CallbackPath { get; set; } = "/authentication/login-callback";

    /// <summary>
    /// Gets or sets the sign-out callback path.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><description>Blazor Server typically uses <c>/signout-callback-oidc</c>.</description></item>
    /// <item><description>Blazor WebAssembly uses <c>/authentication/logout-callback</c>.</description></item>
    /// </list>
    /// </remarks>
    public string SignedOutCallbackPath { get; set; } = "/authentication/logout-callback";

    /// <summary>
    /// Gets or sets whether to get claims from the user info endpoint.
    /// </summary>
    /// <remarks>
    /// When enabled, the OIDC handler calls the provider's <c>userinfo</c> endpoint after authentication.
    /// Some providers or app registrations may return 401 from <c>userinfo</c> unless specific permissions/scopes
    /// are configured.
    /// </remarks>
    public bool GetClaimsFromUserInfoEndpoint { get; set; } = false;

    /// <summary>
    /// Gets or sets the metadata address (optional, auto-discovered from Authority if not set).
    /// </summary>
    public string? MetadataAddress { get; set; }

    /// <summary>
    /// Gets or sets the base URL of the application (primarily for Blazor WebAssembly) used to build absolute redirect URIs.
    /// </summary>
    /// <remarks>
    /// Microsoft Entra ID requires <c>redirect_uri</c> to be an absolute URI. In many cases the framework can infer the base URI,
    /// but if your host setup or reverse proxying causes relative redirect URIs to be sent, set this to the app origin, e.g.
    /// <c>https://localhost:9009</c>.
    /// </remarks>
    public string AppBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="OidcOptions"/> class.
    /// </summary>
    public OidcOptions()
    {
        // Set default OIDC scopes
        Scopes = ["openid", "profile", "offline_access"];
    }
}
