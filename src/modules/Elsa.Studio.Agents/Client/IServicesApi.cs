using Elsa.Agents;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Refit;

namespace Elsa.Studio.Agents.Client;

///  Represents a client API for interacting with AI services.
public interface IServicesApi
{
    /// Lists all services.
    [Get("/ai/services")]
    Task<ListResponse<ServiceModel>> ListAsync(CancellationToken cancellationToken = default);
    
    /// Gets a service by ID.
    [Get("/ai/services/{id}")]
    Task<ServiceModel> GetAsync(string id, CancellationToken cancellationToken = default);
    
    /// Creates a new service.
    [Post("/ai/services")]
    Task<ServiceModel> CreateAsync(ServiceInputModel request, CancellationToken cancellationToken = default);
    
    /// Updates a service.
    [Post("/ai/services/{id}")]
    Task<ServiceModel> UpdateAsync(string id, ServiceInputModel request, CancellationToken cancellationToken = default);

    /// Deletes a service.
    [Delete("/ai/services/{id}")]
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    /// Deletes multiple services.
    [Post("/ai/bulk-actions/services/delete")]
    Task<BulkDeleteResponse> BulkDeleteAsync(BulkDeleteRequest request, CancellationToken cancellationToken = default);

    /// Checks if a name is unique.
    Task<GetIsNameUniqueResponse> GetIsNameUniqueAsync(IsUniqueNameRequest request, CancellationToken cancellationToken = default);
}