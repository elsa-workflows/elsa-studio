namespace Elsa.Studio.Persistence;

using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a hierarchical graph.
/// </summary>
/// <typeparam name="T">The type of the nodes.</typeparam>
public class HierarchicalGraph<T> where T : notnull
{
    private readonly Dictionary<T, T?> _parentMap = new();
    private readonly Dictionary<T, List<T>> _childrenMap = new();

    // Add a parent-child relationship
    public void AddRelationship(T? parent, T child)
    {
        if (parent != null)
        {
            if (!_childrenMap.ContainsKey(parent))
            {
                _childrenMap[parent] = new List<T>();
            }
            _childrenMap[parent].Add(child);
        }

        if (child != null)
        {
            _parentMap[child] = parent;
        }
    }

    // Remove a parent-child relationship
    public void RemoveRelationship(T parent, T child)
    {
        if (parent != null && _childrenMap.ContainsKey(parent))
        {
            _childrenMap[parent].Remove(child);
        }
        _parentMap.Remove(child);
    }

    // Get the parent of a given child
    public T GetParent(T child)
    {
        _parentMap.TryGetValue(child, out var parent);
        return parent;
    }

    // Get the children of a given parent
    public IEnumerable<T> GetChildren(T parent)
    {
        if (parent != null && _childrenMap.ContainsKey(parent))
        {
            return _childrenMap[parent];
        }
        return Enumerable.Empty<T>();
    }
    
    // Get ancestors of a given child
    public IEnumerable<T?> GetAncestors(T child)
    {
        var ancestors = new List<T?>();
        var current = child;

        while (current != null! && _parentMap.TryGetValue(current, out var parent))
        {
            ancestors.Add(parent);
            current = parent;
        }

        return ancestors;
    }

    // Check if a node is a parent
    public bool IsParent(T node)
    {
        return _childrenMap.ContainsKey(node);
    }

    // Check if a node is a child
    public bool IsChild(T node)
    {
        return _parentMap.ContainsKey(node);
    }

    // Get all nodes
    public IEnumerable<T> GetAllNodes()
    {
        var nodes = new HashSet<T>(_parentMap.Keys);
        foreach (var children in _childrenMap.Values)
        {
            nodes.UnionWith(children);
        }
        return nodes;
    }
}
