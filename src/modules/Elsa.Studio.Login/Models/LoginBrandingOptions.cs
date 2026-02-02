namespace Elsa.Studio.Login.Models;

/// <summary>
/// Configuration model for branding and version information used on the login page.
/// </summary>
public class LoginBrandingOptions
{
    public const string SectionName = "Branding";
    
    /// <summary>
    /// Application name displayed on the login page.
    /// </summary>
    public string AppName { get; set; } = "Elsa Studio";
    
    /// <summary>
    /// Application tagline displayed on the login page.
    /// </summary>
    public string AppTagline { get; set; } = "Workflow Management";
    
    /// <summary>
    /// Logo URL for the login page.
    /// </summary>
    public string LogoUrl { get; set; } = "/logo.png";
    
    /// <summary>
    /// Client version displayed on the login page.
    /// </summary>
    public string ClientVersion { get; set; } = "3.x";
    
    /// <summary>
    /// Server version displayed on the login page.
    /// </summary>
    public string ServerVersion { get; set; } = "3.x";
    
    /// <summary>
    /// Default client version to show if configuration is missing.
    /// </summary>
    public string DefaultClientVersion { get; set; } = "Elsa Studio";
    
    /// <summary>
    /// Default server version to show if configuration is missing.
    /// </summary>
    public string DefaultServerVersion { get; set; } = "3.x";
}