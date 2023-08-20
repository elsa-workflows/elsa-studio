using System.Net;
using Elsa.Api.Client.Resources.Features.Contracts;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A feature service that uses a remote backend to retrieve feature flags.
/// </summary>
public class RemoteFeatureService : IFeatureService
{
    private readonly IRemoteBackendApiClientProvider _remoteBackendApiClientProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteFeatureService"/> class.
    /// </summary>
    public RemoteFeatureService(IRemoteBackendApiClientProvider remoteBackendApiClientProvider)
    {
        _remoteBackendApiClientProvider = remoteBackendApiClientProvider;
    }

    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        var api = await _remoteBackendApiClientProvider.GetApiAsync<IFeaturesApi>(cancellationToken);

        try
        {
            _ = await api.GetAsync(featureName, cancellationToken);
            return true;
        }
        catch (ApiException e) when(e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
    }
}