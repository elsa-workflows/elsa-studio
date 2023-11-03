using Elsa.Api.Client.Contracts;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Abstractions;

/// <summary>
/// A provider of a syntax.
/// </summary>
public abstract class SyntaxProviderBase : ISyntaxProvider
{
    /// <inheritdoc />
    public virtual string SyntaxName => GetType().Name.Replace("SyntaxProvider", "");

    /// <inheritdoc />
    public abstract Type ExpressionType { get; }

    /// <inheritdoc />
    public IDictionary<string, object> CustomProperties { get; } = new Dictionary<string, object>();

    /// <inheritdoc />
    public abstract IExpression CreateExpression(object? value = default);
}