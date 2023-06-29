using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;
using Elsa.Studio.Abstractions;

namespace Elsa.Studio.SyntaxProviders;

/// <summary>
/// A provider of a JavaScript syntax that is used by Monaco.
/// </summary>
public class JavaScriptSyntaxProvider : MonacoSyntaxProviderBase
{
    public override Type ExpressionType => typeof(JavaScriptExpression);
    public override string Language => "javascript";

    public override IExpression CreateExpression(object? value = default) => new JavaScriptExpression(value as string ?? "");
}