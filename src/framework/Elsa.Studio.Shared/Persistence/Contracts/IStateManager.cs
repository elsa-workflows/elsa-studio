namespace Elsa.Studio.Persistence;

public interface IStateManager
{
    void RegisterComponent(IPersistentComponent component);
    void UnregisterComponent(IPersistentComponent component);
    IPersistentComponent GetCurrentLeaf();
}