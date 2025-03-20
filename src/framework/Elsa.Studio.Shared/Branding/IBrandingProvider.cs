namespace Elsa.Studio.Branding;

/// <summary>
/// Provides branding information for an application, including its name, logo, and reverse logo.
/// </summary>
public interface IBrandingProvider
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    string AppName { get; }

    /// <summary>
    /// Logo on white background
    /// </summary>
    string? LogoUrl { get; }

    /// <summary>
    /// Logo on dark background
    /// </summary>
    string? LogoReverseUrl { get; }
}
