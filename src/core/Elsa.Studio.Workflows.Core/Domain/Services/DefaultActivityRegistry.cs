using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

public class DefaultActivityRegistry : IActivityRegistry
{
    private readonly IActivityRegistryProvider _provider;
    private Dictionary<(string ActivityTypeName, int Version), ActivityDescriptor> _activityDescriptors = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public DefaultActivityRegistry(IActivityRegistryProvider provider)
    {
        _provider = provider;
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var descriptors = await _provider.ListAsync(cancellationToken);
        _activityDescriptors = descriptors.ToDictionary(x => (x.TypeName, x.Version));
    }

    public async Task EnsureLoadedAsync(CancellationToken cancellationToken = default)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            if (_activityDescriptors.Any())
                return;

            await RefreshAsync(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public IEnumerable<ActivityDescriptor> List()
    {
        // Return the latest version of each activity descriptor from _activityDescriptors.
        return _activityDescriptors.Values
            .Where(x => x.IsBrowsable)
            .GroupBy(activityDescriptor => activityDescriptor.TypeName)
            .Select(grouping => grouping.OrderByDescending(y => y.Version).First());
    }

    public ActivityDescriptor? Find(string activityType, int? version = default)
    {
        version ??= 1;
        return _activityDescriptors.TryGetValue((activityType, version.Value), out var descriptor) ? descriptor : null;
    }
}