using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Services;

/// <summary>
/// Provides the default implementation of the app bar service.
/// </summary>
public class DefaultAppBarService : IAppBarService
{
    private readonly ICollection<RenderFragment> _appBarItems = new List<RenderFragment>();
    private int _appBarSequence;
    
    /// <summary>
    /// Occurs when the set of app bar items has changed.
    /// </summary>
    public event Action? AppBarItemsChanged;
    /// <summary>
    /// Gets the configured app bar items.
    /// </summary>
    public IEnumerable<RenderFragment> AppBarItems => _appBarItems.ToList();
    
    /// <summary>
    /// Adds the specified component to the application bar.
    /// </summary>
    /// <typeparam name="T">The type of component to add.</typeparam>
    public void AddAppBarItem<T>() where T : IComponent
    {
        _appBarItems.Add(builder => builder.CreateComponent<T>(ref _appBarSequence));
        AppBarItemsChanged?.Invoke();
    }
}