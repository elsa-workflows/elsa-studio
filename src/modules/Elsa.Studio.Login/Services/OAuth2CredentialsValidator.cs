using System.Net.Http.Json;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// Validates OAuth2 credentials using a token endpoint, client ID, and optional client secret.
/// </summary>
public class OAuth2CredentialsValidator(IOptions<OAuth2CredentialsValidatorOptions> options, OAuth2HttpClient httpClient) : ICredentialsValidator
{
    /// <inheritdoc />
    public async ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var tokenResponse = await httpClient.RequestTokenAsync(username, password, cancellationToken);

        // If the access token is null or empty, the validation failed.
        if (string.IsNullOrEmpty(tokenResponse.AccessToken))
            return new(false, null, null);

        // Return the result with the access token and refresh token.
        return new(true, tokenResponse.AccessToken, tokenResponse.RefreshToken);
    }
}

/// <summary>
/// Represents configuration options for the OAuth2 credentials validation process.
/// </summary>
public class OAuth2CredentialsValidatorOptions
{
    /// <summary>
    /// The URL of the OAuth2 token endpoint.
    /// </summary>
    public string TokenEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// The client ID to use for the OAuth2 client.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The client secret to use for the OAuth2 client (if confidential).
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// The scope of access being requested during the OAuth2 authentication process.
    /// Typically, this defines the permissions or resources the client is requesting
    /// authorization for.
    /// </summary>
    public string? Scope { get; set; }
}

public class OAuth2HttpClient(HttpClient httpClient, IOptions<OAuth2CredentialsValidatorOptions> options)
{
    private readonly OAuth2CredentialsValidatorOptions _options = options.Value;
    
    /// <summary>
    /// Sends a token request to the OAuth2 token endpoint using the specified credentials.
    /// </summary>
    /// <param name="username">The username to authenticate with.</param>
    /// <param name="password">The password to authenticate with.</param>
    /// <param name="cancellationToken">A cancellation token used to cancel the operation.</param>
    /// <returns>A <see cref="TokenResponse"/> containing the access token, refresh token, and other token details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the response cannot be read or contains no valid data.</exception>
    public async Task<TokenResponse> RequestTokenAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint);
        var content = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "username", username },
            { "password", password },
            { "client_id", _options.ClientId },
        };
        
        if (!string.IsNullOrEmpty(_options.ClientSecret)) content["client_secret"] = _options.ClientSecret;
        if (!string.IsNullOrEmpty(_options.Scope)) content["scope"] = _options.Scope;
        
        request.Content = new FormUrlEncodedContent(content);

        var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken) ?? throw new InvalidOperationException("Failed to read token response.");
    }
}