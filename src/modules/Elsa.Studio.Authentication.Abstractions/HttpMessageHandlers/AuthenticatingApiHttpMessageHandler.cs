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

        // Check if BackendApiScopes are configured for OIDC multi-audience scenarios
        string[]? scopes = null;

        var backendOptions = sp.GetService<IOptions<BackendApiScopeOptions>>()?.Value;
        if (backendOptions?.Scopes?.Length > 0)
        {
            scopes = backendOptions.Scopes;
        }

        // Request token with backend API scopes if configured, otherwise use default
        var accessToken = scopes?.Length > 0
            ? await authenticationProvider.GetAccessTokenAsync(scopes, cancellationToken)
            : await authenticationProvider.GetAccessTokenAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = null;
        else
            request.Headers.Authorization = new("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}
