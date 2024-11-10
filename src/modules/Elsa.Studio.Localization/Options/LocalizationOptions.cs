using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Localization.Options
{
    public class LocalizationOptions
    {
        public static string LocalizationSection = "Localization";
        public string[] SupportedCultures { get; set; } = Array.Empty<string>();
    }
}
