using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Studio.Login.HttpMessageHandlers;

/// <summary>
/// An <see cref="HttpMessageHandler"/> that configures the outgoing HTTP request to use the access token as bearer token.
/// </summary>
public class AuthenticatingApiHttpMessageHandler(IRemoteBackendAccessor remoteBackendAccessor, IBlazorServiceAccessor blazorServiceAccessor)
    : DelegatingHandler
{
    /// <inheritdoc />
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var sp = blazorServiceAccessor.Services;
        var jwtAccessor = sp.GetRequiredService<IJwtAccessor>();
        var accessToken = await jwtAccessor.ReadTokenAsync(TokenNames.AccessToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Refresh token and retry once.
            var tokens = await RefreshTokenAsync(jwtAccessor, cancellationToken);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            // Retry.
            response = await base.SendAsync(request, cancellationToken);
        }

        return response;
    }

    private async Task<LoginResponse> RefreshTokenAsync(IJwtAccessor jwtAccessor, CancellationToken cancellationToken)
    {
        // Get refresh token.
        var refreshToken = await jwtAccessor.ReadTokenAsync(TokenNames.RefreshToken);
        
        // Setup request to get new tokens.
        var url = remoteBackendAccessor.RemoteBackend.Url + "/identity/refresh-token";
        var refreshRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        refreshRequestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshToken);
        
        // Send request.
        var response = await base.SendAsync(refreshRequestMessage, cancellationToken);

        // If the refresh token is invalid, we can't do anything.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return new LoginResponse(false, null, null);

        // Parse response into tokens.
        var tokens = (await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken))!;
        
        // Store tokens.
        await jwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, tokens.RefreshToken!);
        await jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, tokens.AccessToken!);
        
        // Return tokens.
        return tokens;
    }
}