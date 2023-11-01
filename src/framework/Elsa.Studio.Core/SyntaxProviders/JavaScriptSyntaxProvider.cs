using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;
using Elsa.Studio.Abstractions;

namespace Elsa.Studio.SyntaxProviders;

/// <summary>
/// A provider of a JavaScript syntax that is used by Monaco.
/// </summary>
public class JavaScriptSyntaxProvider : MonacoSyntaxProviderBase
{
    /// <inheritdoc />
    public override Type ExpressionType => typeof(JavaScriptExpression);

    /// <inheritdoc />
    public override string Language => "javascript";

    /// <inheritdoc />
    public override IExpression CreateExpression(object? value = default) => new JavaScriptExpression(value as string ?? "");
}