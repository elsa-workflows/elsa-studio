using Elsa.Studio.Models;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides functionality for managing app bar items in the application.
/// </summary>
public interface IAppBarService
{
    /// <summary>
    /// Invoked when the app bar items change.
    /// </summary>
    event Action AppBarItemsChanged;

    /// <summary>
    /// Gets the collection of elements displayed in the app bar.
    /// </summary>
    IEnumerable<AppBarElement> AppBarElements { get; }
    
    /// <summary>
    /// A collection of components to render in the app bar.
    /// </summary>
    IEnumerable<RenderFragment> AppBarComponents { get; }
    
    /// <summary>
    /// Adds a component to the app bar.
    /// </summary>
    /// <typeparam name="T">The type of the component.</typeparam>
    [Obsolete("Use AddElement instead.")]
    void AddAppBarItem<T>() where T : IComponent;

    /// <summary>
    /// Adds a component of the specified type to the app bar.
    /// </summary>
    /// <typeparam name="T">The type of the component to add.</typeparam>
    void AddComponent<T>(float? order = null) where T : IComponent;

    /// <summary>
    /// Adds an element of the specified type to the app bar.
    /// </summary>
    void AddElement<T>(float? order = null) where T : AppBarElement, new();

    /// <summary>
    /// Adds an element to the app bar.
    /// </summary>
    /// <param name="element">The app bar element to add.</param>
    void AddElement(AppBarElement element);

    /// <summary>
    /// Removes all components of the specified type from the app bar.
    /// </summary>
    /// <typeparam name="T">The type of the component to remove.</typeparam>
    void RemoveComponent<T>() where T : IComponent;

    /// <summary>
    /// Removes all elements with the specified order from the app bar.
    /// </summary>
    /// <param name="order">The order value of elements to remove.</param>
    void RemoveElementsByOrder(float order);

    /// <summary>
    /// Clears all elements from the app bar.
    /// </summary>
    void ClearAll();
}