namespace Elsa.Studio.Contracts;

/// <summary>
/// A provider of a syntax that is used by Monaco.
/// </summary>
public interface IMonacoSyntaxProvider : ISyntaxProvider
{
    /// <summary>
    /// The language to use when configuring monaco.
    /// </summary>
    public string Language { get; }
}