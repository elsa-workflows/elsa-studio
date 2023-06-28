using Elsa.Studio.Workflows.Models;

namespace Elsa.Studio.Workflows.Contracts;

public interface IActivityDisplaySettingsProvider
{
    IDictionary<string, ActivityDisplaySettings> GetSettings();
}