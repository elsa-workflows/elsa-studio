using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Models;

namespace Elsa.Studio.Workflows.Contracts;

public interface IActivityPortService
{
    IActivityPortProvider GetProvider(string activityType);
    IEnumerable<Port> GetPorts(PortProviderContext context);
}