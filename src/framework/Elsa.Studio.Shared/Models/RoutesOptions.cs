namespace Elsa.Studio.Shared.Models;

/// <summary>
/// Configuration model for application routes and navigation paths.
/// </summary>
public class RoutesOptions
{
    public const string SectionName = "Routes";
    
    /// <summary>
    /// Path to the login page.
    /// </summary>
    public string LoginPath { get; set; } = "/login";
    
    /// <summary>
    /// Path to the authentication logout endpoint.
    /// </summary>
    public string AuthenticationLogoutPath { get; set; } = "/authentication/logout";
    
    /// <summary>
    /// Path to the home page (used for redirect after login).
    /// </summary>
    public string HomePage { get; set; } = "/";
}