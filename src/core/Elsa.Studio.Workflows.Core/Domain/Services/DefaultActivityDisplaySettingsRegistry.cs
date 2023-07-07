using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Domain.Services;

public class DefaultActivityDisplaySettingsRegistry : IActivityDisplaySettingsRegistry
{
    private readonly IEnumerable<IActivityDisplaySettingsProvider> _providers;
    private IDictionary<string, ActivityDisplaySettings>? _settings;

    public DefaultActivityDisplaySettingsRegistry(IEnumerable<IActivityDisplaySettingsProvider> providers)
    {
        _providers = providers;
    }

    public static ActivityDisplaySettings DefaultSettings { get; set; } = new(DefaultActivityColors.Default);

    public ActivityDisplaySettings GetSettings(string activityType)
    {
        var dictionary = GetSettingsDictionary();
        return dictionary.TryGetValue(activityType, out var settings) ? settings : DefaultSettings;
    }

    private IDictionary<string, ActivityDisplaySettings> GetSettingsDictionary()
    {
        if(_settings != null)
            return _settings;
        
        var settings = new Dictionary<string, ActivityDisplaySettings>();

        foreach (var provider in _providers)
        {
            var providerSettings = provider.GetSettings();
            
            foreach (var (activityType, activityDisplaySettings) in providerSettings)
                settings[activityType] = activityDisplaySettings;
        }
        
        _settings = settings;
        return _settings;
    }
}