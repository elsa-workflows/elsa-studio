using Elsa.Studio.Workflows.Models;

namespace Elsa.Studio.Workflows.Contracts;

/// <summary>
/// Provides mappings between activity types and icons.
/// </summary>
public interface IActivityDisplaySettingsRegistry
{
    ActivityDisplaySettings GetSettings(string activityType);
}