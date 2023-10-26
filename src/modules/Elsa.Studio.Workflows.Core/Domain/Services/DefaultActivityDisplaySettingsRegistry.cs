using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class DefaultActivityDisplaySettingsRegistry : IActivityDisplaySettingsRegistry
{
    private readonly IEnumerable<IActivityDisplaySettingsProvider> _providers;
    private IDictionary<string, ActivityDisplaySettings>? _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultActivityDisplaySettingsRegistry"/> class.
    /// </summary>
    public DefaultActivityDisplaySettingsRegistry(IEnumerable<IActivityDisplaySettingsProvider> providers)
    {
        _providers = providers;
    }

    private static ActivityDisplaySettings DefaultSettings { get; set; } = new(DefaultActivityColors.Default);

    /// <inheritdoc />
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