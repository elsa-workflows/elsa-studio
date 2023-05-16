using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Models;

namespace Elsa.Studio.Environments.Services;

public class DefaultEnvironmentService : IEnvironmentService
{
    public event Action? EnvironmentsChanged;
    public event Action? CurrentEnvironmentChanged;

    public ServerEnvironment? CurrentEnvironment { get; private set; }
    public IEnumerable<ServerEnvironment> Environments { get; private set; } = new List<ServerEnvironment>();

    public void SetEnvironments(IEnumerable<ServerEnvironment> environments, string? defaultEnvironmentName = null)
    {
        var environmentList = environments.ToList();
        Environments = environmentList;

        if (defaultEnvironmentName != null)
            SetCurrentEnvironment(defaultEnvironmentName);
        else if (CurrentEnvironment == null)
            SetCurrentEnvironment(environmentList.FirstOrDefault()?.Name ?? string.Empty);

        EnvironmentsChanged?.Invoke();
    }

    public void SetCurrentEnvironment(string name)
    {
        var environments = Environments.ToList();
        var environment = environments.FirstOrDefault(x => x.Name == name);

        if (environment == null || CurrentEnvironment == environment)
            return;

        CurrentEnvironment = environment;
        CurrentEnvironmentChanged?.Invoke();
    }
}