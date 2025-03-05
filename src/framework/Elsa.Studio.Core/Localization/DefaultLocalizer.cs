using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Localization;

/// <inheritdoc />
public class DefaultLocalizer(ILocalizationProvider provider) : ILocalizer
{
    /// <inheritdoc />
    public LocalizedString this[string key]
    {
        get
        {
            if (string.IsNullOrWhiteSpace(key))
                return new LocalizedString(string.Empty, string.Empty, true);

            var notFound = false;
            var translation = provider.GetTranslation(key);

            if (string.IsNullOrEmpty(translation))
            {
                translation = key;
                notFound = true;
            }

            return new LocalizedString(key, translation, notFound);
        }
    }
}