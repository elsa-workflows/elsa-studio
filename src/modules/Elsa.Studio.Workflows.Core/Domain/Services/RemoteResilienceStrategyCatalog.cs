using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.Resilience.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class RemoteResilienceStrategyCatalog(IBackendApiClientProvider backendApiClientProvider) : IResilienceStrategyCatalog
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<JsonObject>> ListAsync(string category, CancellationToken cancellationToken = default)
    {
        var api = await backendApiClientProvider.GetApiAsync<IResilienceStrategiesApi>(cancellationToken);
        var response = await api.ListAsync(category, cancellationToken);

        return response.Items;
    }
}