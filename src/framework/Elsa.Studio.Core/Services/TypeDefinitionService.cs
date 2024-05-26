using Elsa.Api.Client.Resources.Scripting.Contracts;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

public  class TypeDefinitionService : ITypeDefinition
{
    private readonly IRemoteBackendApiClientProvider _remoteBackendApiClientProvider;

    private readonly IServiceProvider _serviceProvider;
    private readonly IBlazorServiceAccessor _blazorServiceAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteExpressionProvider"/> class.
    /// </summary>
    public TypeDefinitionService(IRemoteBackendApiClientProvider remoteBackendApiClientProvider
        , IBlazorServiceAccessor blazorServiceAccessor
        , IServiceProvider serviceProvider)
    {
        _remoteBackendApiClientProvider = remoteBackendApiClientProvider;
        _blazorServiceAccessor = blazorServiceAccessor;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public async Task<string> GetTypeDefinition(string definitionId, string activityTypeName, string propertyName, CancellationToken cancellationToken = default)
    {
        _blazorServiceAccessor.Services = _serviceProvider;
        var api = await _remoteBackendApiClientProvider.GetApiAsync<IJavaScriptApi>(cancellationToken);
        HttpResponseMessage data = await api.GetTypeDefinitions(definitionId
            , new Api.Client.Resources.Scripting.Requests.GetWorkflowJavaScriptDefinitionRequest(definitionId, activityTypeName, propertyName)
            , cancellationToken);

        return await data.Content.ReadAsStringAsync();
    }
}