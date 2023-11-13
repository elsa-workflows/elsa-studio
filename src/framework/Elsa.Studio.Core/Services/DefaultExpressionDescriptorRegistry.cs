using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultExpressionDescriptorRegistry : IExpressionDescriptorRegistry
{
    private readonly IDictionary<string, ExpressionDescriptor> _descriptors = new Dictionary<string, ExpressionDescriptor>();

    /// <inheritdoc />
    public void Add(ExpressionDescriptor descriptor)
    {
        _descriptors[descriptor.Type] = descriptor;
    }

    /// <inheritdoc />
    public void AddRange(IEnumerable<ExpressionDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
            Add(descriptor);
    }

    /// <inheritdoc />
    public IEnumerable<ExpressionDescriptor> List()
    {
        return _descriptors.Values;
    }

    /// <inheritdoc />
    public ExpressionDescriptor? FindByType(string type)
    {
        return _descriptors.TryGetValue(type, out var descriptor) ? descriptor : default;
    }

    /// <inheritdoc />
    public ExpressionDescriptor? Find(Func<ExpressionDescriptor, bool> predicate)
    {
        return _descriptors.Values.FirstOrDefault(predicate);
    }
}