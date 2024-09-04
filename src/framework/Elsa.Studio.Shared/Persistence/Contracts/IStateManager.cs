namespace Elsa.Studio.Persistence;

/// <summary>
/// A service that manages the state of components.
/// </summary>
public interface IStateManager
{
    /// <summary>
    /// Loads the state of the component.
    /// </summary>
    Task LoadStateAsync(IPersistentComponent component);
    
    /// <summary>
    /// Saves the state of the component.
    /// </summary>
    Task SaveStateAsync(IPersistentComponent component);
}