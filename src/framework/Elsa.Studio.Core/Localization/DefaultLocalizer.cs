using Elsa.Studio.Contracts;
using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Localization
{
    /// <inheritdoc />
    public class DefaultLocalizer(ILocalizationProvider provider) : ILocalizer
    {
        /// <inheritdoc />
        public LocalizedString this[string key]
        {
            get
            {
                var notFound = false;
                var translation = provider.GetTranslation(key);

                if (translation is null)
                {
                    translation = key;
                    notFound = true;
                }

                return new LocalizedString(key, translation, notFound);
            }
        }
    }
}
