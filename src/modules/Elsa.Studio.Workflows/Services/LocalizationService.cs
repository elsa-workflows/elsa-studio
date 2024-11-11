using Elsa.Studio.Resources;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Workflows.Services
{
    public class LocalizationService
    {
        private readonly IStringLocalizer _localizer;
        public LocalizationService(IStringLocalizerFactory factory)
        {
            var type = typeof(ResourceLocalization);
            _localizer = factory.Create(type);
        }
        public LocalizedString this[string key] => _localizer[key];
    }
}
