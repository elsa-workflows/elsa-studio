using Elsa.Studio.Workflows.UI.Models;

namespace Elsa.Studio.Workflows.UI.Contracts;

public interface IActivityDisplaySettingsProvider
{
    IDictionary<string, ActivityDisplaySettings> GetSettings();
}