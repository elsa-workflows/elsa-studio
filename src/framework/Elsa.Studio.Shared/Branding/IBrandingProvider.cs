using Elsa.Studio.Components;
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
    string AppName { get; }

    /// <summary>
    /// Logo on white background
    /// </summary>
    string? LogoUrl { get; }

    /// <summary>
    /// Logo on dark background
    /// </summary>
    string? LogoReverseUrl { get; }

    /// <summary>
    /// The component to render. Use this to completely customize the branding component.
    /// </summary>
    RenderFragment Branding =>
        builder =>
        {
            builder.OpenComponent(0, typeof(DefaultBranding));
            builder.AddAttribute(1, "Provider", this);
            builder.CloseComponent();
        };
}