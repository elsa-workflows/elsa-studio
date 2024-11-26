using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Localization;

/// <inheritdoc />
public class DefaultLocalizer(ILocalizationProvider provider) : ILocalizer
{
    /// <inheritdoc />
    public LocalizedString this[string key] => new(key, key, false);
}