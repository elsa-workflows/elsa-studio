using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text;

namespace Elsa.Studio.Login.Services;

/// <inheritdoc/>
public class OpenIdConnectAuthorizationService(IJwtAccessor jwtAccessor, IOptions<OpenIdConnectConfiguration> configuration, NavigationManager navigationManager, HttpClient httpClient, IOpenIdConnectPkceStateService pkceStateService) : IAuthorizationService
{
    /// <inheritdoc/>
    public async Task RedirectToAuthorizationServer()
    {
        var config = configuration.Value;
        var redirectUri = new Uri(navigationManager.Uri).GetLeftPart(UriPartial.Authority) + "/signin-oidc";
        string url = config.AuthEndpoint + $"?client_id={WebUtility.UrlEncode(config.ClientId)}&redirect_uri={WebUtility.UrlEncode(redirectUri)}&response_type=code&scope={WebUtility.UrlEncode(String.Join(' ', config.Scopes))}";
        if (config.UsePkce)
        {
            var generated = await pkceStateService.GeneratePkceCodeChallenge();
            url += $"&code_challenge={generated.CodeChallenge}&code_challenge_method={generated.Method}";
        }
        if (navigationManager.ToBaseRelativePath(navigationManager.Uri) is { } returnUrl and not "/")
        {
            url += "&state=" + WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(returnUrl));
        }

        navigationManager.NavigateTo(url, true);
    }

    /// <inheritdoc/>
    public async Task ReceiveAuthorizationCode(string code, string? state, CancellationToken cancellationToken)
    {
        var config = configuration.Value;
        var redirectUri = new Uri(navigationManager.Uri).GetLeftPart(UriPartial.Authority) + "/signin-oidc";

        var formValues = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("client_id", config.ClientId),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("redirect_uri", redirectUri)
        };
        if (config.UsePkce)
        {
            var codeVerifier = await pkceStateService.GetPkceCodeVerifier();
            formValues.Add(new KeyValuePair<string, string>("code_verifier", codeVerifier));
        }

        var refreshRequestMessage = new HttpRequestMessage(HttpMethod.Post, config.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(formValues)
        };

        // Send request.
        var response = await httpClient.SendAsync(refreshRequestMessage, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"""
                
                OIDC token endpoint returned an error response. This can occur if the authorization 
                code is invalid or has already been used, or if there is a misconfiguration in the OIDC provider. 
                Please check the OIDC provider's logs for more details on the error.

                Token endpoint URL: {config.TokenEndpoint}
                Status Code: {response.StatusCode}
                ReasonPhrase: {response.ReasonPhrase},
                Error: {await response.Content.ReadAsStringAsync(cancellationToken)}
                """, null, response.StatusCode);
        }

        var tokens = (await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken))!;

        await jwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, tokens.RefreshToken ?? "");
        await jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, tokens.AccessToken ?? "");
        await jwtAccessor.WriteTokenAsync(TokenNames.IdToken, tokens.IdToken ?? "");

        string returnUrl = "/";
        if (!String.IsNullOrWhiteSpace(state))
        {
            returnUrl = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(state));
        }
        navigationManager.NavigateTo(returnUrl, true);
    }
}
