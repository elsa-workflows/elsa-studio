namespace Elsa.Studio.Authentication.OpenIdConnect.BlazorServer.Models;

/// <summary>
/// Effective configuration to use for performing a refresh-token grant.
/// </summary>
public class OidcRefreshConfiguration(string tokenEndpoint, string clientId, string? clientSecret)
{
    public string TokenEndpoint { get; } = tokenEndpoint;
    public string ClientId { get; } = clientId;
    public string? ClientSecret { get; } = clientSecret;
}