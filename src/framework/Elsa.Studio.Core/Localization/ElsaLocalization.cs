using Microsoft.Extensions.Localization;
using Elsa.Studio.Resources;
using System.Globalization;

namespace Elsa.Studio.Localization
{
    public class ElsaLocalization
    {
        private readonly ILocalizationProvider _provider;

        public ElsaLocalization(ILocalizationProvider provider)
        {
            _provider = provider;
        }

        public LocalizedString this[string key]
        {
            get
            {
                bool notFound = false;

                var translation = _provider.GetTranslation(key);

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
