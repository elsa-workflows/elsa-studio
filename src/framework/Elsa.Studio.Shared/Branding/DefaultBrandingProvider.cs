using Elsa.Studio.Components;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Branding;

/// <summary>
/// Provides default branding information for the application, including the application name, logo URL, and reverse logo URL.
/// Implements the <see cref="Elsa.Studio.Branding.IBrandingProvider"/> interface.
/// </summary>
public class DefaultBrandingProvider : IBrandingProvider
{
    /// <inheritdoc />
    public virtual string AppName => $"Elsa {ToolVersion.GetDisplayVersion()}";

    /// <inheritdoc />
    public virtual string LogoUrl => GetLogoUrl(false);

    /// <inheritdoc />
    public virtual string LogoReverseUrl => GetLogoUrl(true);

    private string GetLogoUrl(bool v)
    {
        return "_content/Elsa.Studio.Shell/img/icon.png";
    }
    
    /// <summary>
    /// Provides the render fragment.
    /// </summary>
    public virtual RenderFragment Branding =>
        builder =>
        {
            builder.OpenComponent(0, typeof(DefaultBranding));
            builder.AddAttribute(1, "Provider", this);
            builder.CloseComponent();
        };
}