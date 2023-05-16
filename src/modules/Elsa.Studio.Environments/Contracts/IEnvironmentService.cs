using Elsa.Studio.Environments.Models;

namespace Elsa.Studio.Environments.Contracts;

/// <summary>
/// Manages the environment in which the dashboard is running.
/// </summary>
public interface IEnvironmentService
{
    /// <summary>
    /// Raised when the current environment changed.
    /// </summary>
    event Action CurrentEnvironmentChanged;
    
    /// <summary>
    /// The current environment.
    /// </summary>
    WorkflowsEnvironment? CurrentEnvironment { get; }
    
    /// <summary>
    /// Gets or sets a list of environments.
    /// </summary>
    IEnumerable<WorkflowsEnvironment> Environments { get; set; }

    /// <summary>
    /// Sets the current environment.
    /// </summary>
    /// <param name="name">The name of the current environment to set.</param>
    void SetCurrentEnvironment(string name);
}