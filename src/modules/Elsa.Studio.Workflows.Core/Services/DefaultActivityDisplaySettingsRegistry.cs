using Elsa.Studio.Workflows.Core.Contracts;
using Elsa.Studio.Workflows.Core.Models;

namespace Elsa.Studio.Workflows.Core.Services;

public class DefaultActivityDisplaySettingsRegistry : IActivityDisplaySettingsRegistry
{
    private readonly IDictionary<string, ActivityDisplaySettings> _settings = new Dictionary<string, ActivityDisplaySettings>(DefaultActivityDisplaySettings.Settings);

    public DefaultActivityDisplaySettingsRegistry()
    {
        DefaultSettings = DefaultActivityDisplaySettings.DefaultSettings;
    }

    public ActivityDisplaySettings DefaultSettings { get; set; }
    
    public void ConfigureActivitySettings(string activityType, ActivityDisplaySettings settings) => _settings[activityType] = settings;

    public ActivityDisplaySettings GetSettings(string activityType) => _settings.TryGetValue(activityType, out var settings) ? settings : DefaultSettings;
}