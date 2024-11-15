using System.Globalization;
using Elsa.Studio.Resources;

namespace Elsa.Studio.Localization
{
    internal class DefaultLocalizationProvider : ILocalizationProvider
    {
        public string? GetTranslation(string key)
        {
            return ResourceLocalization.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
        }
    }
}
