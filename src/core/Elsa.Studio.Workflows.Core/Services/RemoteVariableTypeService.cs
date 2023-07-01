using Elsa.Api.Client.Resources.VariableTypes.Contracts;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Extensions;
using Elsa.Studio.Workflows.Contracts;

namespace Elsa.Studio.Workflows.Services;

public class RemoteVariableTypeService : IVariableTypeService
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;

    public RemoteVariableTypeService(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }
    
    public async Task<IEnumerable<VariableTypeDescriptor>> GetVariableTypesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _backendConnectionProvider.GetApi<IVariableTypesApi>().ListAsync(cancellationToken);
        return response.Items;
    }
}