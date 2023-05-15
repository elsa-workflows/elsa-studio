namespace Elsa.Dashboard.Contracts;

/// <summary>
/// Represents a module that can be registered with the dashboard.
/// </summary>
public interface IModule
{
    /// <summary>
    /// Called by the dashboard to initialize the module.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token.</param>
    ValueTask InitializeAsync(CancellationToken cancellationToken = default);
}