using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;

namespace Elsa.Studio.Workflows.Domain.Contracts;

public interface IActivityPortService
{
    IActivityPortProvider GetProvider(PortProviderContext context);
    IEnumerable<Port> GetPorts(PortProviderContext context);
}