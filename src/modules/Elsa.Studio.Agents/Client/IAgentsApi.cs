using Elsa.Agents;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Studio.Agents.Client;

///  Represents a client API for interacting with agents.
public interface IAgentsApi
{
    /// Lists all agents.
    [Post("/ai/agents/list")]
    Task<ListResponse<AgentModel>> ListAsync(CancellationToken cancellationToken = default);
    
    /// Generates a unique name for an agent.
    [Post("/ai/actions/agents/generate-unique-name")]
    Task<GenerateUniqueNameResponse> GenerateUniqueNameAsync(CancellationToken cancellationToken = default);
    
    /// Checks if a name is unique for an agent.
    [Post("/ai/queries/agents/is-unique-name")]
    Task<IsUniqueNameResponse> GetIsNameUniqueAsync(IsUniqueNameRequest request, CancellationToken cancellationToken);
}