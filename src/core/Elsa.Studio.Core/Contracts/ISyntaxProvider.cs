using Elsa.Api.Client.Contracts;

namespace Elsa.Studio.Contracts;

/// <summary>
/// A provider of a syntax.
/// </summary>
public interface ISyntaxProvider
{
    /// <summary>
    /// The syntax name that is used by the expression.
    /// </summary>
    public string SyntaxName { get; }
    
    /// <summary>
    /// The expression type that supports the syntax.
    /// </summary>
    Type ExpressionType { get; }

    /// <summary>
    /// A bag of custom properties.
    /// </summary>
    public IDictionary<string, object> CustomProperties { get; }

    /// <summary>
    /// Creates an expression.
    /// </summary>
    /// <param name="value">The value.</param>
    IExpression CreateExpression(object? value = default!);
}