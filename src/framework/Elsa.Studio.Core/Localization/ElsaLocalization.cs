using Microsoft.Extensions.Localization;
using Elsa.Studio.Resources;
using System.Globalization;

namespace Elsa.Studio.Localization
{
    public class ElsaLocalization
    {
        public LocalizedString this[string key]
        {
            get
            {
                bool notFound = false;

                var translation = ResourceLocalization.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);

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
