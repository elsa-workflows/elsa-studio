using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.Localization.Options;

/// <summary>
/// Options for localization settings.
/// </summary>
public class LocalizationOptions
{
    public static string LocalizationSection = "Localization";

    /// <summary>
    /// Gets or sets the supported cultures.
    /// </summary>
    public string[] SupportedCultures { get; set; } = { "en-US" };
}