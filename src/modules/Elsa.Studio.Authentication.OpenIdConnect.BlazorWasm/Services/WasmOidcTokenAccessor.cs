using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Elsa.Studio.Authentication.OpenIdConnect.Models;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Services;

/// <summary>
/// Blazor WASM implementation of <see cref="IOidcTokenAccessor"/> that uses the built-in token provider.
/// </summary>
public class WasmOidcTokenAccessor : IOidcTokenAccessor
{
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly OidcOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasmOidcTokenAccessor"/> class.
    /// </summary>
    public WasmOidcTokenAccessor(IAccessTokenProvider tokenProvider, IOptions<OidcOptions> options)
    {
        _tokenProvider = tokenProvider;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        // For WASM, we use the IAccessTokenProvider to get the current access token
        // The framework handles token refresh automatically
        
        // Map token names to what the framework expects
        if (tokenName == "access_token" || string.Equals(tokenName, "accessToken", StringComparison.OrdinalIgnoreCase))
        {
            // Get all resource scopes (excluding standard OIDC scopes)
            // This is critical for Azure AD which requires explicit scopes during token requests
            var standardScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
            { 
                "openid", "profile", "email", "offline_access" 
            };
            
            var resourceScopes = _options.Scopes
                .Where(s => !standardScopes.Contains(s))
                .ToArray();
            
            // Request token with explicit scopes to ensure Azure AD receives the scope parameter
            // in both authorization and token exchange requests
            AccessTokenResult tokenResult;
            if (resourceScopes.Length > 0)
            {
                tokenResult = await _tokenProvider.RequestAccessToken(new AccessTokenRequestOptions
                {
                    Scopes = resourceScopes
                });
            }
            else
            {
                // Fallback to default scopes if no resource scopes configured
                tokenResult = await _tokenProvider.RequestAccessToken();
            }
            
            if (tokenResult.TryGetToken(out var token))
            {
                return token.Value;
            }
        }
        
        // For other token types (id_token, refresh_token), we can't directly access them
        // in WASM for security reasons - they're managed by the authentication framework
        return null;
    }
}
