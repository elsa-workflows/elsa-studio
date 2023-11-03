using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;
using Elsa.Studio.Abstractions;

namespace Elsa.Studio.SyntaxProviders;

/// <summary>
/// A provider of a C# syntax that is used by Monaco.
/// </summary>
public class CSharpSyntaxProvider : MonacoSyntaxProviderBase
{
    /// <inheritdoc />
    public override Type ExpressionType => typeof(CSharpExpression);

    /// <inheritdoc />
    public override string Language => "csharp";

    /// <inheritdoc />
    public override IExpression CreateExpression(object? value = default) => new CSharpExpression(value as string ?? "");
}