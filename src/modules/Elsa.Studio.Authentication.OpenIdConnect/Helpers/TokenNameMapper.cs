namespace Elsa.Studio.Authentication.OpenIdConnect.Helpers;

/// <summary>
/// Helper class for mapping between Elsa token names and OIDC token name conventions.
/// </summary>
public static class TokenNameMapper
{
    /// <summary>
    /// Maps an Elsa token name to the corresponding OIDC token name convention.
    /// </summary>
    /// <param name="tokenName">The Elsa token name to map.</param>
    /// <returns>The OIDC token name.</returns>
    public static string MapToOidcTokenName(string tokenName)
    {
        return tokenName switch
        {
            TokenNames.AccessToken => "access_token",
            TokenNames.IdToken => "id_token",
            TokenNames.RefreshToken => "refresh_token",
            _ => tokenName
        };
    }
}
