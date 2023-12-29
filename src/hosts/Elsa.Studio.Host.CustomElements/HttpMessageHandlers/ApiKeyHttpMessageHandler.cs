using System.Net.Http.Headers;
using Elsa.Studio.Host.CustomElements.Services;

namespace Elsa.Studio.Host.CustomElements.HttpMessageHandlers;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that configures the outgoing HTTP request to use the configured API key or bearer token.
/// </summary>
public class AuthHttpMessageHandler : DelegatingHandler
{
    private readonly BackendService _backendService;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthHttpMessageHandler"/> class.
    /// </summary>
    public AuthHttpMessageHandler(BackendService backendService)
    {
        _backendService = backendService;
    }

    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var apiKey = _backendService.ApiKey;
        var accessToken = _backendService.AccessToken;
        
        if(!string.IsNullOrEmpty(apiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);
        else if(!string.IsNullOrEmpty(accessToken))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        return await base.SendAsync(request, cancellationToken);
    }
}