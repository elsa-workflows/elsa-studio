using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;

namespace Elsa.Studio.Workflows.Domain.Contracts;

public interface IActivityPortService
{
    IActivityPortProvider GetProvider(string activityType);
    IEnumerable<Port> GetPorts(PortProviderContext context);
}