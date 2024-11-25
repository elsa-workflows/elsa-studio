namespace Elsa.Studio.Localization.Options;

/// <summary>
/// Options for localization settings.
/// </summary>
public class LocalizationOptions
{
    public static string LocalizationSection = "Localization";

    /// <summary>
    /// Gets or sets the supported cultures.
    /// </summary>
    public string[] SupportedCultures { get; set; } = ["en-US"];
}