using System.Globalization;
using Elsa.Studio.Localization;

namespace Elsa.Studio.Translations
{
    internal class ResxLocalizationProvider : ILocalizationProvider
    {
        public string? GetTranslation(string key)
        {
            return Elsa.Studio.Translations.Translations.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        }
    }
}
