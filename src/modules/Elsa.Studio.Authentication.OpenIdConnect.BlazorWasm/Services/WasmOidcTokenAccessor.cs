using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Services;

/// <summary>
/// Blazor WASM implementation of <see cref="IOidcTokenAccessor"/> that uses the built-in token provider.
/// </summary>
public class WasmOidcTokenAccessor : IOidcTokenAccessor
{
    private readonly IAccessTokenProvider _tokenProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WasmOidcTokenAccessor"/> class.
    /// </summary>
    public WasmOidcTokenAccessor(IAccessTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    /// <inheritdoc />
    public async Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        // For WASM, we use the IAccessTokenProvider to get the current access token
        // The framework handles token refresh automatically
        
        // Map token names to what the framework expects
        if (tokenName == "access_token" || string.Equals(tokenName, "accessToken", StringComparison.OrdinalIgnoreCase))
        {
            var tokenResult = await _tokenProvider.RequestAccessToken();
            
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
