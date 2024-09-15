using Elsa.Api.Client.Resources.ActivityDescriptors.Contracts;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.ActivityDescriptors.Requests;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// An activity registry provider that uses a remote backend to retrieve activity descriptors.
/// </summary>
public class RemoteActivityRegistryProvider : IActivityRegistryProvider
{
    private readonly IBackendApiClientProvider _backendApiClientProvider;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteActivityRegistryProvider"/> class.
    /// </summary>
    public RemoteActivityRegistryProvider(IBackendApiClientProvider backendApiClientProvider)
    {
        _backendApiClientProvider = backendApiClientProvider;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        var api = await _backendApiClientProvider.GetApiAsync<IActivityDescriptorsApi>(cancellationToken);
        var request = new ListActivityDescriptorsRequest
        {
            Refresh = true
        };
        var response = await api.ListAsync(request, cancellationToken);
        
        return response.Items;
    }
}