using Elsa.Agents;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Studio.Agents.Client;

///  Represents a client API for interacting with AI plugins.
public interface IPluginsApi
{
    /// Lists all services.
    [Get("/ai/plugins")]
    Task<ListResponse<PluginDescriptorModel>> ListAsync(CancellationToken cancellationToken = default);
}