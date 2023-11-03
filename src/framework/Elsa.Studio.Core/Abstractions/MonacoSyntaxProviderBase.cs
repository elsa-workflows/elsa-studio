using Elsa.Studio.Contracts;

namespace Elsa.Studio.Abstractions;

/// <summary>
/// A provider of a syntax that is used by Monaco.
/// </summary>
public abstract class MonacoSyntaxProviderBase : SyntaxProviderBase, IMonacoSyntaxProvider
{
    /// <inheritdoc />
    public abstract string Language { get; }
}