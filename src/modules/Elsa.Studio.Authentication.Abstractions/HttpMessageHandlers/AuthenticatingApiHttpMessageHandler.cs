using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.Abstractions.Models;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.Abstractions.HttpMessageHandlers;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that attaches an access token (if available) to outgoing HTTP requests.
/// </summary>
/// <remarks>
/// This handler is authentication-provider-agnostic and does not attempt to refresh tokens itself.
/// Token acquisition/refresh is the responsibility of the active <see cref="IAuthenticationProvider"/> implementation
/// (e.g. OIDC via MSAL/RemoteAuthenticationService, ElsaAuth via stored JWTs, etc.).
/// </remarks>
public class AuthenticatingApiHttpMessageHandler(IBlazorServiceAccessor blazorServiceAccessor) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sp = blazorServiceAccessor.Services;
        var authenticationProvider = sp.GetService<IAuthenticationProvider>();

        if (authenticationProvider == null)
            return await base.SendAsync(request, cancellationToken);

        // Try to get scopes from TokenPurposeOptions (legacy) or from the provider's AuthenticationOptions
        string[]? scopes = null;

        var purposeOptions = sp.GetService<IOptions<TokenPurposeOptions>>()?.Value;
        if (purposeOptions != null)
        {
            // Legacy path: TokenPurposeOptions still configured separately
            purposeOptions.ScopesByPurpose.TryGetValue(purposeOptions.BackendApiPurpose, out scopes);
        }

        // Request token with backend API scopes if configured
        // Note: IAuthenticationProvider now has a default implementation that delegates to the non-scoped version
        // when scopes are not supported by the provider
        var accessToken = await authenticationProvider.GetAccessTokenAsync(TokenNames.AccessToken, scopes, cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = null;
        else
            request.Headers.Authorization = new("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
