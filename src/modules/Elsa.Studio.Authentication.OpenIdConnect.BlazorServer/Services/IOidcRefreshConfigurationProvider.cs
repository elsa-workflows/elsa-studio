using Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;

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