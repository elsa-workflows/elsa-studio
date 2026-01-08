using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.Contracts;
using System.Net;
using System.Net.Http.Json;

namespace Elsa.Studio.Authentication.ElsaAuth.Services;

///<inheritdoc/>
public class ElsaIdentityRefreshTokenService(IRemoteBackendAccessor remoteBackendAccessor, IJwtAccessor jwtAccessor, HttpClient httpClient) : IRefreshTokenService
{
    ///<inheritdoc/>
    public async Task<LoginResponse> RefreshTokenAsync(CancellationToken cancellationToken)
    {
        // Get refresh token.
        var refreshToken = await jwtAccessor.ReadTokenAsync(TokenNames.RefreshToken);

        // Setup request to get new tokens.
        var url = remoteBackendAccessor.RemoteBackend.Url + "/identity/refresh-token";
        var refreshRequestMessage = new HttpRequestMessage(HttpMethod.Post, url);
        refreshRequestMessage.Headers.Authorization = new("Bearer", refreshToken);

        // Send request.
        var response = await httpClient.SendAsync(refreshRequestMessage, cancellationToken);

        // If the refresh token is invalid, we can't do anything.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            return new(false, null, null);

        // Parse response into tokens.
        var tokens = (await response.Content.ReadFromJsonAsync<LoginResponse>(cancellationToken: cancellationToken))!;

        // Store tokens.
        await jwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, tokens.RefreshToken!);
        await jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, tokens.AccessToken!);

        // Return tokens.
        return tokens;
    }
}

