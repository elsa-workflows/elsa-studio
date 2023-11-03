namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides syntax services.
/// </summary>
public interface ISyntaxService
{
    /// <summary>
    /// Lists all syntax providers.
    /// </summary>
    IEnumerable<ISyntaxProvider> ListSyntaxProviders();
    
    /// <summary>
    /// Lists all syntaxes.
    /// </summary>
    /// <returns></returns>
    IEnumerable<string> ListSyntaxes();
    
    /// <summary>
    /// Gets a syntax provider by name.
    /// </summary>
    ISyntaxProvider GetSyntaxProviderByName(string syntaxName);
    
    /// <summary>
    /// Gets a syntax provider by expression type.
    /// </summary>
    ISyntaxProvider GetSyntaxProviderByExpressionType(Type expressionType);
}