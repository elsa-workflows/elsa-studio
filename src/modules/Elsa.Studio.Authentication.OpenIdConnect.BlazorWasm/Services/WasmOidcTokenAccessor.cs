using Elsa.Studio.Authentication.OpenIdConnect.Contracts;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorWasm.Services;

/// <summary>
/// Blazor WASM implementation of <see cref="IOidcTokenAccessor"/> that uses the built-in token provider.
/// </summary>
/// <remarks>
/// This accessor supports scope-aware token requests, enabling incremental consent scenarios
/// where different tokens are needed for different API audiences (e.g., Graph vs. backend API).
/// </remarks>
public class WasmOidcTokenAccessor : IOidcTokenAccessorWithScopes
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
    public Task<string?> GetTokenAsync(string tokenName, CancellationToken cancellationToken = default)
    {
        // Forward to scoped overload with null scopes (use default behavior)
        return GetTokenAsync(tokenName, scopes: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetTokenAsync(string tokenName, IEnumerable<string>? scopes, CancellationToken cancellationToken = default)
    {
        // For WASM, we use the IAccessTokenProvider to get the current access token
        // The framework handles token refresh automatically

        // Map token names to what the framework expects
        if (string.Equals(tokenName, "access_token", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tokenName, "accessToken", StringComparison.OrdinalIgnoreCase))
        {
            // If specific scopes are requested, use them (e.g., for backend API calls)
            // Otherwise, request with default scopes (e.g., for Graph/userinfo calls)
            var requestedScopes = scopes?.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

            var tokenResult = requestedScopes?.Length > 0
                ? await _tokenProvider.RequestAccessToken(new AccessTokenRequestOptions
                {
                    Scopes = requestedScopes
                })
                : await _tokenProvider.RequestAccessToken();

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
