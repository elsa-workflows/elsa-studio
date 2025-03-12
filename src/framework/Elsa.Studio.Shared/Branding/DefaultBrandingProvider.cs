namespace Elsa.Studio.Branding;

public class DefaultBrandingProvider : IBrandingProvider
{
    public virtual  string AppName => $"Elsa {ToolVersion.GetDisplayVersion()}";

    public virtual string? LogoUrl => GetLogoUrl(false);

    public virtual string? LogoReverseUrl => GetLogoUrl(true);

    private string? GetLogoUrl(bool v)
    {
        return "_content/Elsa.Studio.Shell/img/icon.png";
    }
} 