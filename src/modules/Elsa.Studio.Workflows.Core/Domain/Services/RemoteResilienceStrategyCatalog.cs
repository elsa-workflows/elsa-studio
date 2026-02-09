using System.Net;
using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.Resilience.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class RemoteResilienceStrategyCatalog(IBackendApiClientProvider backendApiClientProvider) : IResilienceStrategyCatalog
{
    /// <inheritdoc />
    public async ValueTask<IEnumerable<JsonObject>> ListAsync(string category, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await backendApiClientProvider.GetApiAsync<IResilienceStrategiesApi>(cancellationToken);
            var response = await api.ListAsync(category, cancellationToken);

            return response.Items;
        }
        catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            // The Resilience feature is not enabled in Elsa Server. Return an empty list.
            return [];
        }
    }
}