using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Environments.Services;

/// <summary>
/// Selects the default environment before the remote feature catalog is fetched.
/// </summary>
internal sealed class EnvironmentAwareRemoteFeatureProvider(
    IRemoteFeatureProvider inner,
    EnvironmentLoader environmentLoader) : IRemoteFeatureProvider
{
    /// <inheritdoc />
    public async Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
    {
        await environmentLoader.EnsureLoadedAsync(cancellationToken);
        return await inner.IsEnabledAsync(featureName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default)
    {
        await environmentLoader.EnsureLoadedAsync(cancellationToken);
        return await inner.ListAsync(cancellationToken);
    }
}
