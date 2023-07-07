using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Extensions;

public static class ActivityRegistryExtensions
{
    public static async Task<IDictionary<string, ActivityDescriptor>> GetDictionaryAsync(this IActivityRegistry activityRegistry, CancellationToken cancellationToken = default) =>
        (await activityRegistry.ListAsync(cancellationToken)).ToDictionary(x => x.TypeName);
}