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

        string? accessToken;

        // Check if the provider supports scoped token requests (OIDC providers)
        if (authenticationProvider is IScopedAccessTokenProvider scopedProvider)
        {
            // Get token purpose configuration
            var purposeOptions = sp.GetService<IOptions<TokenPurposeOptions>>()?.Value;

            string[]? scopes = null;
            
            // Get scopes for the backend API purpose
            purposeOptions?.ScopesByPurpose.TryGetValue(purposeOptions.BackendApiPurpose, out scopes);

            // Request token with backend API scopes if configured
            accessToken = await scopedProvider.GetAccessTokenAsync(TokenNames.AccessToken, scopes, cancellationToken);
        }
        else
        {
            // Non-scoped providers (e.g., ElsaAuth JWT provider)
            // These use a single token for all backend calls
            accessToken = await authenticationProvider.GetAccessTokenAsync(TokenNames.AccessToken, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = null;
        else
            request.Headers.Authorization = new("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
