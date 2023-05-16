using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Models;

namespace Elsa.Studio.Environments.Services;

public class DefaultEnvironmentService : IEnvironmentService
{
    public event Action? CurrentEnvironmentChanged;

    public WorkflowsEnvironment? CurrentEnvironment { get; private set; }
    public IEnumerable<WorkflowsEnvironment> Environments { get; set; } = new List<WorkflowsEnvironment>();
    
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