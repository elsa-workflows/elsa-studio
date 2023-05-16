namespace Elsa.Studio.Contracts;

/// <summary>
/// Manages modules.
/// </summary>
public interface IModuleService
{
    /// <summary>
    /// Returns all modules.
    /// </summary>
    /// <returns>All modules.</returns>
    IEnumerable<IModule> GetModules();

    /// <summary>
    /// Initializes all modules.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task InitializeModulesAsync(CancellationToken cancellationToken = default);
}