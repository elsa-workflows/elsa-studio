using Elsa.Studio.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Authentication.Abstractions.HttpMessageHandlers;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that attaches an access token (if available) to outgoing HTTP requests.
/// </summary>
/// <remarks>
/// This handler is intentionally authentication-provider-agnostic. It relies on <see cref="IAuthenticationProvider"/>
/// (resolved from the current Blazor scope) to retrieve an access token.
/// </remarks>
public class BearerTokenHttpMessageHandler(IBlazorServiceAccessor blazorServiceAccessor) : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sp = blazorServiceAccessor.Services;
        var authenticationProvider = sp.GetRequiredService<IAuthenticationProvider>();

        await AttachAccessTokenAsync(request, authenticationProvider, cancellationToken);

        return await base.SendAsync(request, cancellationToken);
    }

    private static async Task AttachAccessTokenAsync(HttpRequestMessage request, IAuthenticationProvider authenticationProvider, CancellationToken cancellationToken)
    {
        var accessToken = await authenticationProvider.GetAccessTokenAsync(TokenNames.AccessToken, cancellationToken);

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization = null;
            return;
        }

        request.Headers.Authorization = new("Bearer", accessToken);
    }
}

