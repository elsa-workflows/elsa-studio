using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// Provides mappings between activity types and icons.
/// </summary>
public interface IActivityDisplaySettingsProvider
{
    /// <summary>
    /// Returns a dictionary of activity type to display settings.
    /// </summary>
    /// <returns></returns>
    IDictionary<string, ActivityDisplaySettings> GetSettings();
}