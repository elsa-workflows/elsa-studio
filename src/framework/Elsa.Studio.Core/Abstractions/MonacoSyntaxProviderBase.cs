using Elsa.Studio.Contracts;

namespace Elsa.Studio.Abstractions;

public abstract class MonacoSyntaxProviderBase : SyntaxProviderBase, IMonacoSyntaxProvider
{
    public abstract string Language { get; }
}