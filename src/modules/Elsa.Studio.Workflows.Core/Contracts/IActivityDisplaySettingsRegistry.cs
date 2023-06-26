using Elsa.Studio.Workflows.Core.Models;

namespace Elsa.Studio.Workflows.Core.Contracts;

/// <summary>
/// Provides mappings between activity types and icons.
/// </summary>
public interface IActivityDisplaySettingsRegistry
{
    ActivityDisplaySettings GetSettings(string activityType);
}