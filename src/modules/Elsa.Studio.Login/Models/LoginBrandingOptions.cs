namespace Elsa.Studio.Login.Models;

/// <summary>
/// Configuration model for branding and version information used on the login page.
/// Used to override default IBrandingProvider values when needed.
/// </summary>
public class LoginBrandingOptions
{
    public const string SectionName = "Branding";
    
    /// <summary>
    /// Application name displayed on the login page.
    /// If not specified, uses IBrandingProvider.AppName.
    /// </summary>
    public string? AppName { get; set; }
    
    /// <summary>
    /// Application tagline displayed on the login page.
    /// If not specified, uses IBrandingProvider.AppTagline.
    /// </summary>
    public string? AppTagline { get; set; }
    
    /// <summary>
    /// Logo URL for the login page.
    /// If not specified, uses IBrandingProvider.LogoUrl with smart path resolution.
    /// </summary>
    public string? LogoUrl { get; set; }
    
    /// <summary>
    /// Client version displayed on the login page.
    /// </summary>
    public string ClientVersion { get; set; } = "3.x";
    
    /// <summary>
    /// Server version displayed on the login page.
    /// </summary>
    public string ServerVersion { get; set; } = "3.x";
}