using Elsa.Studio.Localization.Options;

namespace Elsa.Studio.Localization.Models;

public class LocalizationConfig
{
    public Action<LocalizationOptions> ConfigureLocalizationOptions { get; set; } = _ => { };
}