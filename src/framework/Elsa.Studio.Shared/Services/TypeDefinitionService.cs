using Elsa.Api.Client.Resources.Scripting.Contracts;
using Elsa.Api.Client.Resources.Scripting.Requests;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <summary>
/// A service that provides TypeScript type definitions.
/// </summary>
public class TypeDefinitionService(IRemoteBackendApiClientProvider remoteBackendApiClientProvider)
{
    /// <summary>
    /// Gets the type definition for the specified activity type.
    /// </summary>
    public async Task<string> GetTypeDefinition(string definitionId, string activityTypeName, string propertyName, CancellationToken cancellationToken = default)
    {
        var api = await remoteBackendApiClientProvider.GetApiAsync<IJavaScriptApi>(cancellationToken);
        var data = await api.GetTypeDefinitions(definitionId, new GetWorkflowJavaScriptDefinitionRequest(definitionId, activityTypeName, propertyName), cancellationToken);
        return await data.Content.ReadAsStringAsync(cancellationToken);
    }
}