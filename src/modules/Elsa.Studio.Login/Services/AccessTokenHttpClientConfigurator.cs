using System.Net.Http.Headers;
using Elsa.Api.Client.Contracts;
using Elsa.Studio.Authentication.JwtBearer.Contracts;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// An <see cref="IApiHttpRequestConfigurator"/> that configures the outgoing HTTP request to use the access token as bearer token.
/// </summary>
public class AccessTokenHttpClientConfigurator : IApiHttpRequestConfigurator
{
    private readonly IJwtAccessor _jwtAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccessTokenHttpClientConfigurator"/> class.
    /// </summary>
    public AccessTokenHttpClientConfigurator(IJwtAccessor jwtAccessor)
    {
        _jwtAccessor = jwtAccessor;
    }

    /// <inheritdoc />
    public async Task ConfigureRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await _jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}