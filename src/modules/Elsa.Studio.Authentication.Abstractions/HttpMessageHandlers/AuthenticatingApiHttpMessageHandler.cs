using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

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
        var authenticationProvider = sp.GetRequiredService<IAuthenticationProvider>();

        var accessToken = await authenticationProvider.GetAccessTokenAsync(TokenNames.AccessToken, cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
            request.Headers.Authorization = null;
        else
            request.Headers.Authorization = new("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}

