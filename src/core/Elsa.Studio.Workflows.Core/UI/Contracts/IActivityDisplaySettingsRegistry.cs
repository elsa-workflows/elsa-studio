using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.UI.Contracts;

/// <summary>
/// Provides mappings between activity types and icons.
/// </summary>
public interface IActivityDisplaySettingsRegistry
{
    ActivityDisplaySettings GetSettings(string activityType);
}