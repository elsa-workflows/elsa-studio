using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Localization;

/// <summary>
/// Interface for localization.
/// </summary>
public interface ILocalizer
{
    /// <summary>
    /// Gets the localized string for the given key.
    /// </summary>
    /// <param name="key"></param>
    LocalizedString this[string key] { get; }
}