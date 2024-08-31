using Elsa.Agents;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Studio.Agents.Client;

///  Represents a client API for interacting with agents.
public interface IApiKeysApi
{
    /// Lists all API keys.
    [Get("/ai/api-keys")]
    Task<ListResponse<ApiKeyModel>> ListAsync(CancellationToken cancellationToken = default);
    
    /// Gets an API key by ID.
    [Get("/ai/api-keys/{id}")]
    Task<ApiKeyModel> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Creates a new API key.
    [Post("/ai/api-keys")]
    Task<ApiKeyModel> CreateAsync(ApiKeyInputModel request, CancellationToken cancellationToken = default);
    
    /// Updates an API key.
    [Post("/ai/api-keys/{id}")]
    Task<ApiKeyModel> UpdateAsync(string id, ApiKeyInputModel request, CancellationToken cancellationToken = default);

    /// Deletes an API key.
    [Delete("/ai/api-keys/{id}")]
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// Deletes multiple API keys.
    [Post("/ai/bulk-actions/api-keys/delete")]
    Task<BulkDeleteApiKeysResponse> BulkDeleteAsync(BulkDeleteApiKeysRequest request, CancellationToken cancellationToken = default);

    /// Checks if a name is unique.
    Task<GetIsNameUniqueResponse> GetIsNameUniqueAsync(IsUniqueNameRequest request, CancellationToken cancellationToken = default);
}