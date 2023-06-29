using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;
using Elsa.Studio.Abstractions;

namespace Elsa.Studio.SyntaxProviders;

/// <summary>
/// A provider of a JavaScript syntax that is used by Monaco.
/// </summary>
public class LiquidSyntaxProvider : MonacoSyntaxProviderBase
{
    public override Type ExpressionType => typeof(LiquidExpression);
    public override string Language => "liquid";

    public override IExpression CreateExpression(object? value = default) => new LiquidExpression(value as string ?? "");
}