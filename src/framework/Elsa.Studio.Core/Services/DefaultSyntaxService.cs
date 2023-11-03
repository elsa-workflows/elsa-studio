using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultSyntaxService : ISyntaxService
{
    private readonly IEnumerable<ISyntaxProvider> _providers;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultSyntaxService"/> class.
    /// </summary>
    public DefaultSyntaxService(IEnumerable<ISyntaxProvider> providers)
    {
        _providers = providers;
    }

    /// <inheritdoc />
    public IEnumerable<ISyntaxProvider> ListSyntaxProviders() => _providers;

    /// <inheritdoc />
    public IEnumerable<string> ListSyntaxes() => _providers.Select(x => x.SyntaxName);

    /// <inheritdoc />
    public ISyntaxProvider GetSyntaxProviderByName(string syntaxName) => _providers.FirstOrDefault(x => x.SyntaxName == syntaxName) ?? throw new Exception($"No syntax provider found for syntax '{syntaxName}'");

    /// <inheritdoc />
    public ISyntaxProvider GetSyntaxProviderByExpressionType(Type expressionType) => _providers.FirstOrDefault(x => x.ExpressionType == expressionType) ?? throw new Exception($"No syntax provider found for expression type '{expressionType}'");
}