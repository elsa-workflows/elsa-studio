using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Localization
{
    public interface ILocalizationProvider
    {
        string? GetTranslation(string key);
    }
}
