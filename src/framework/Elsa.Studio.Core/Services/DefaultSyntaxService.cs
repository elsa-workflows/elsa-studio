using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

public class DefaultSyntaxService : ISyntaxService
{
    private readonly IEnumerable<ISyntaxProvider> _providers;

    public DefaultSyntaxService(IEnumerable<ISyntaxProvider> providers)
    {
        _providers = providers;
    }
    
    public IEnumerable<ISyntaxProvider> ListSyntaxProviders() => _providers;
    public IEnumerable<string> ListSyntaxes() => _providers.Select(x => x.SyntaxName);
    public ISyntaxProvider GetSyntaxProviderByName(string syntaxName) => _providers.FirstOrDefault(x => x.SyntaxName == syntaxName) ?? throw new Exception($"No syntax provider found for syntax '{syntaxName}'");
    public ISyntaxProvider GetSyntaxProviderByExpressionType(Type expressionType) => _providers.FirstOrDefault(x => x.ExpressionType == expressionType) ?? throw new Exception($"No syntax provider found for expression type '{expressionType}'");
}