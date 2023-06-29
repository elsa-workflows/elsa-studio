using Elsa.Api.Client.Contracts;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Abstractions;

public abstract class SyntaxProviderBase : ISyntaxProvider
{
    public virtual string SyntaxName => GetType().Name.Replace("SyntaxProvider", "");
    public abstract Type ExpressionType { get; }
    public IDictionary<string, object> CustomProperties { get; } = new Dictionary<string, object>();
    public abstract IExpression CreateExpression(object? value = default);
}