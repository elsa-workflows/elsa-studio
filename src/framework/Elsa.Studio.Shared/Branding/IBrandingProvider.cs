using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Branding;

/// <summary>
/// Provides branding information for an application, including its name, logo, and reverse logo.
/// </summary>
public interface IBrandingProvider
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    [Obsolete("Use Branding instead to provide a custom component, or configure the DefaultBrandingProvider with an AppName.")] string AppName { get; }

    /// <summary>
    /// Logo on a white background.
    /// </summary>
    [Obsolete("Use Branding instead to provide a custom component, or configure the DefaultBrandingProvider with a LogoUrl.")] string? LogoUrl { get; }

    /// <summary>
    /// Logo on a dark background
    /// </summary>
    [Obsolete("Use Branding instead to provide a custom component, or configure the DefaultBrandingProvider with a LogoReverseUrl.")] string? LogoReverseUrl { get; }

    /// <summary>
    /// The component to render. Use this to completely customize the branding component.
    /// </summary>
    RenderFragment Branding { get; }
}