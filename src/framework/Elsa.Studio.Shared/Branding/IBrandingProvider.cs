using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Branding;

public interface IBrandingProvider
{
    string AppName { get; }

    /// <summary>
    /// Logo on white background
    /// </summary>
    string? LogoUrl { get; }

    /// <summary>
    /// Logo on dark background
    /// </summary>
    string? LogoReverseUrl { get; }
}
