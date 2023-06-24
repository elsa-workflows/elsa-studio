using Elsa.Studio.Workflows.Core.Models;

namespace Elsa.Studio.Workflows.Core.Contracts;

/// <summary>
/// Provides mappings between activity types and icons.
/// </summary>
public interface IActivityDisplaySettingsRegistry
{
    ActivityDisplaySettings DefaultSettings { get; set; }
    void ConfigureActivitySettings(string activityType, ActivityDisplaySettings settings);
    ActivityDisplaySettings GetSettings(string activityType);
}