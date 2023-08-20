namespace Elsa.Studio.Contracts;

public interface ISyntaxService
{
    IEnumerable<ISyntaxProvider> ListSyntaxProviders();
    IEnumerable<string> ListSyntaxes();
    ISyntaxProvider GetSyntaxProviderByName(string syntaxName);
    ISyntaxProvider GetSyntaxProviderByExpressionType(Type expressionType);
}