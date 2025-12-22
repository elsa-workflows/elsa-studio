using Elsa.Studio.Branding.Models;
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
    public virtual string AppName => "Elsa Studio";

    /// <inheritdoc />
    public virtual string AppNameWithVersion => $"{AppName} {ToolVersion.GetDisplayVersion()}";

    /// <inheritdoc />
    public virtual string AppTagline => "Clarity for complex orchestration.";

    /// <inheritdoc />
    public virtual string LogoUrl => GetLogoUrl(false);

    /// <inheritdoc />
    public virtual string LogoReverseUrl => GetLogoUrl(true);

    /// <summary>
    /// Provides the render fragment.
    /// </summary>
    public virtual RenderFragment Branding =>
        builder =>
        {
            builder.OpenComponent(0, typeof(DefaultBranding));
            builder.AddAttribute(1, nameof(DefaultBranding.BrandingProvider), this);
            builder.CloseComponent();
        };

    /// <summary>
    /// Represents branding configuration options for the login page.
    /// </summary>
    public virtual LoginBranding Login => new()
    {
        BackgroundUrl = GetLoginBackgroundUrl(false),
        BackgroundReverseUrl = GetLoginBackgroundUrl(true)
    };

    /// <summary>
    /// Represents the default app bar display icons options.
    /// </summary>
    public virtual DefaultAppBarIcons AppBarIcons => new()
    {
        ShowDocumentationLink = true,
        ShowGitHubLink = true
    };

    private string GetLogoUrl(bool reverse)
    {
        var fileName = reverse ? "icon_dark.png" : "icon.png";
        return FindAppFileUrl(fileName, "_content/Elsa.Studio.Shell/img/");
    }

    private string GetLoginBackgroundUrl(bool reverse)
    {
        var fileName = reverse ? "login_background_dark.png" : "login_background_light.png";
        return FindAppFileUrl(fileName, "_content/Elsa.Studio.Shell/img/");
    }

    private string FindAppFileUrl(string fileName, string fallbackPath)
    {
        var fallbackFile = Path.Combine(fallbackPath, fileName);
        try
        {
            var candidates = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", fileName),
                Path.Combine(AppContext.BaseDirectory ?? string.Empty, "wwwroot", "img", fileName),
                Path.Combine(Directory.GetCurrentDirectory(), "img", fileName)
            };

            foreach (var path in candidates)
            {
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                if (File.Exists(path))
                    return "/img/" + fileName;
            }
        }
        catch
        {
            return fallbackFile;
        }

        return fallbackFile;
    }
}