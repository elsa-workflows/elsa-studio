using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Elsa.Studio.Login.Services;

/// <inheritdoc/>
public class OpenIdConnectAuthorizationService(
    IJwtAccessor jwtAccessor, 
    IOptions<OpenIdConnectConfiguration> configuration, 
    NavigationManager navigationManager, 
    HttpClient httpClient, 
    IOpenIdConnectPkceService pkceService,
    IOidcBrowserStateStore browserState,
    ILogger<OpenIdConnectAuthorizationService> logger) : IAuthorizationService
{
    static string CryptoRandom(int bytes = 32) =>
        WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(bytes));
    
    /// <inheritdoc/>
    public async Task RedirectToAuthorizationServer()
    {
        var config = configuration.Value;
        var redirectUri = new Uri(navigationManager.Uri).GetLeftPart(UriPartial.Authority) + "/signin-oidc";

        var returnUrl = navigationManager.ToBaseRelativePath(navigationManager.Uri);
        if (string.IsNullOrWhiteSpace(returnUrl) || returnUrl == "/")
            returnUrl = "/";

        var state = CryptoRandom();
        var nonce = CryptoRandom();

        await browserState.SetAsync($"state:{state}", returnUrl);
        await browserState.SetAsync($"nonce:{state}", nonce); // tie nonce to state

        var query = new Dictionary<string, string?>
        {
            ["client_id"] = config.ClientId,
            ["redirect_uri"] = redirectUri,
            ["response_type"] = "code",
            ["response_mode"] = "query",
            ["scope"] = string.Join(' ', config.Scopes),
            ["state"] = state,
            ["nonce"] = nonce
        };

        if (config.UsePkce)
        {
            // IMPORTANT: your PKCE service should return BOTH verifier + challenge.
            var pkce = await pkceService.GeneratePkceAsync(); // see note below
            await browserState.SetAsync($"pkce:{state}", pkce.CodeVerifier);

            query["code_challenge"] = pkce.CodeChallenge;
            query["code_challenge_method"] = pkce.Method;
        }

        var url = QueryHelpers.AddQueryString(config.AuthEndpoint, query);
        navigationManager.NavigateTo(url, true);
    }


    /// <inheritdoc/>
    public async Task ReceiveAuthorizationCode(string code, string? state, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(state))
            throw new InvalidOperationException("Missing state.");
        
        var config = configuration.Value;
        var returnUrl = await browserState.TakeAsync($"state:{state}") ?? "/";
        var codeVerifier = config.UsePkce ? await browserState.TakeAsync($"pkce:{state}") : null;
        var redirectUri = new Uri(navigationManager.Uri).GetLeftPart(UriPartial.Authority) + "/signin-oidc";

        var formValues = new List<KeyValuePair<string, string>>
        {
            new("client_id", config.ClientId),
            new("grant_type", "authorization_code"),
            new("code", code),
            new("redirect_uri", redirectUri),
            new("scope", string.Join(' ', config.Scopes)) // <-- key for getting API aud
        };

        if (!string.IsNullOrWhiteSpace(config.ClientSecret))
            formValues.Add(new("client_secret", config.ClientSecret));

        if (config.UsePkce)
        {
            if (string.IsNullOrWhiteSpace(codeVerifier))
                throw new InvalidOperationException("Missing PKCE code_verifier.");

            formValues.Add(new("code_verifier", codeVerifier));
        }

        var response = await httpClient.PostAsync(config.TokenEndpoint, new FormUrlEncodedContent(formValues), cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokens = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                     ?? throw new InvalidOperationException("Failed to read token response.");

        await jwtAccessor.WriteTokenAsync(TokenNames.RefreshToken, tokens.RefreshToken ?? "");
        await jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, tokens.AccessToken ?? "");
        await jwtAccessor.WriteTokenAsync(TokenNames.IdToken, tokens.IdToken ?? "");

        navigationManager.NavigateTo(returnUrl, true);
    }

}

public interface IOidcBrowserStateStore
{
    ValueTask SetAsync(string key, string value);
    ValueTask<string?> GetAsync(string key);
    ValueTask RemoveAsync(string key);
    ValueTask<string?> TakeAsync(string key);
}

public class SessionStorageOidcStateStore : IOidcBrowserStateStore
{
    private readonly IJSRuntime _js;

    public SessionStorageOidcStateStore(IJSRuntime js) => _js = js;

    public ValueTask SetAsync(string key, string value) =>
        _js.InvokeVoidAsync("oidcState.set", key, value);

    public ValueTask<string?> GetAsync(string key) =>
        _js.InvokeAsync<string?>("oidcState.get", key);

    public ValueTask RemoveAsync(string key) =>
        _js.InvokeVoidAsync("oidcState.remove", key);

    public async ValueTask<string?> TakeAsync(string key)
    {
        var value = await GetAsync(key);
        if (value != null)
            await RemoveAsync(key);
        return value;
    }
}
