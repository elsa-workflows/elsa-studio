using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;
using Elsa.Studio.Abstractions;

namespace Elsa.Studio.SyntaxProviders;

public class LiteralSyntaxProvider : SyntaxProviderBase
{
    public override Type ExpressionType => typeof(LiteralExpression);
    public override IExpression CreateExpression(object? value = default) => new LiteralExpression(value);
}