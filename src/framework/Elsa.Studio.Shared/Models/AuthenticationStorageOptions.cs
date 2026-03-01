namespace Elsa.Studio.Shared.Models;

/// <summary>
/// Configuration model for authentication token names and storage keys.
/// </summary>
public class AuthenticationStorageOptions
{
    public const string SectionName = "Authentication:StorageKeys";
    
    /// <summary>
    /// Local storage key for authentication tokens.
    /// </summary>
    public string AuthToken { get; set; } = "authToken";
    
    /// <summary>
    /// Local storage key for OIDC user information.
    /// </summary>
    public string OidcUser { get; set; } = "oidc.user";
    
    /// <summary>
    /// Local storage key for user information.
    /// </summary>
    public string User { get; set; } = "user";
    
    /// <summary>
    /// Local storage key for authentication expiry information.
    /// </summary>
    public string AuthExpiry { get; set; } = "authExpiry";
}