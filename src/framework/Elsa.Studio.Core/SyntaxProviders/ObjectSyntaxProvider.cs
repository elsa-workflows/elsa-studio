using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;
using Elsa.Studio.Abstractions;

namespace Elsa.Studio.SyntaxProviders;

public class ObjectSyntaxProvider : SyntaxProviderBase
{
    public override Type ExpressionType => typeof(ObjectExpression);
    public override IExpression CreateExpression(object? value = default) => new ObjectExpression(value as string ?? "");
}