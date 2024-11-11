using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Localization.Services
{
    public interface ICultureService
    {
        Task ChangeCultureAsync(CultureInfo culture);
    }
}
