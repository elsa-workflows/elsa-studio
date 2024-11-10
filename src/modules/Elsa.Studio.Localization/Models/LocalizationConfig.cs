using Elsa.Studio.Localization.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Localization.Models
{
    public class LocalizationConfig
    {
        public Action<LocalizationOptions> ConfigureLocalizationOptions { get; set; } = _ => { };
    }
}
