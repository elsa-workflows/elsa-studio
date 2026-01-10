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
    public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return GetAccessTokenAsync(scopes: null, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(IEnumerable<string>? scopes, CancellationToken cancellationToken = default)
    {
        // For WASM, we use the IAccessTokenProvider to get the current access token
        // The framework handles token refresh automatically

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

        return null;
    }

    /// <inheritdoc />
    public Task<string?> GetIdTokenAsync(CancellationToken cancellationToken = default)
    {
        // ID tokens are not directly accessible in WASM for security reasons
        // They're managed internally by the authentication framework
        return Task.FromResult<string?>(null);
    }

    /// <inheritdoc />
    public Task<string?> GetRefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        // Refresh tokens are not directly accessible in WASM for security reasons
        // They're managed internally by the authentication framework
        return Task.FromResult<string?>(null);
    }
}
