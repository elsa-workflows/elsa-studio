using System.Net;
using Elsa.Api.Client.Resources.Features.Contracts;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Microsoft.Extensions.Logging;
using Refit;

namespace Elsa.Studio.Services;

/// <summary>
/// A feature service that uses a remote backend to retrieve feature flags with built-in resilience.
/// </summary>
public class RemoteFeatureProvider(IBackendApiClientProvider remoteBackendApiClientProvider, ILogger<RemoteFeatureProvider> logger) : IRemoteFeatureProvider
{
    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await remoteBackendApiClientProvider.GetApiAsync<IFeaturesApi>(cancellationToken);
            _ = await api.GetAsync(featureName, cancellationToken);
            return true;
        }
        catch (ApiException e) when(e.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to check if feature {FeatureName} is enabled, assuming disabled", featureName);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var api = await remoteBackendApiClientProvider.GetApiAsync<IFeaturesApi>(cancellationToken);
            var response = await api.ListAsync(cancellationToken);
            logger.LogDebug("Retrieved {Count} features from backend", response.Items.Count());
            return response.Items;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to retrieve remote features, returning empty list");
            return Array.Empty<FeatureDescriptor>();
        }
    }
}