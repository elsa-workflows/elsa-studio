using Elsa.Api.Client.Contracts;
using Elsa.Api.Client.Expressions;
using Elsa.Studio.Abstractions;

namespace Elsa.Studio.SyntaxProviders;

/// <summary>
/// A provider of Python syntax that is used by Monaco.
/// </summary>
public class PythonSyntaxProvider : MonacoSyntaxProviderBase
{
    /// <inheritdoc />
    public override Type ExpressionType => typeof(PythonExpression);

    /// <inheritdoc />
    public override string Language => "python";

    /// <inheritdoc />
    public override IExpression CreateExpression(object? value = default) => new PythonExpression(value as string ?? "");
}