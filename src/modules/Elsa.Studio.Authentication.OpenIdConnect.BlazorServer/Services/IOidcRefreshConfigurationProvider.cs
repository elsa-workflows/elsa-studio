namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Services;

/// <summary>
/// Resolves the effective token endpoint and client credentials to use for server-side OIDC refresh.
/// </summary>
public interface IOidcRefreshConfigurationProvider
{
    /// <summary>
    /// Gets the effective refresh configuration or <c>null</c> if refresh is not possible.
    /// </summary>
    ValueTask<OidcRefreshConfiguration?> GetAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Effective configuration to use for performing a refresh-token grant.
/// </summary>
public class OidcRefreshConfiguration
{
    public OidcRefreshConfiguration(string tokenEndpoint, string clientId, string? clientSecret)
    {
        TokenEndpoint = tokenEndpoint;
        ClientId = clientId;
        ClientSecret = clientSecret;
    }

    public string TokenEndpoint { get; }
    public string ClientId { get; }
    public string? ClientSecret { get; }
}
