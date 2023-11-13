using Elsa.Api.Client.Resources.Scripting.Models;

namespace Elsa.Studio.Contracts;

/// <summary>
/// A registry of available expression descriptors.
/// </summary>
public interface IExpressionDescriptorRegistry
{
    /// <summary>
    /// Adds a descriptor.
    /// </summary>
    void Add(ExpressionDescriptor descriptor);
    
    /// <summary>
    /// Adds a range of descriptors.
    /// </summary>
    void AddRange(IEnumerable<ExpressionDescriptor> descriptors);
    
    /// <summary>
    /// Returns all descriptors.
    /// </summary>
    IEnumerable<ExpressionDescriptor> List();
    
    /// <summary>
    /// Finds a descriptor by type.
    /// </summary>
    ExpressionDescriptor? FindByType(string type);
    
    /// <summary>
    /// Finds a descriptor matching the specified predicate.
    /// </summary>
    ExpressionDescriptor? Find(Func<ExpressionDescriptor, bool> predicate);
}