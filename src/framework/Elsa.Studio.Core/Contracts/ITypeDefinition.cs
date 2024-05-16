namespace Elsa.Studio.Contracts;

public interface ITypeDefinition
{
    Task<string> GetTypeDefinition(string definitionId, string activityTypeName, string propertyName, CancellationToken cancellationToken = default);
}
