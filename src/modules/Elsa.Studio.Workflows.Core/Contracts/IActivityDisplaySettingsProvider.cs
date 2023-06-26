using Elsa.Studio.Workflows.Core.Models;

namespace Elsa.Studio.Workflows.Core.Contracts;

public interface IActivityDisplaySettingsProvider
{
    IDictionary<string, ActivityDisplaySettings> GetSettings();
}