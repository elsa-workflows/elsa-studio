using Elsa.Api.Client.Shared.Models;
using Elsa.Labels.Entities;
using Elsa.Studio.Labels.Models;
using Refit;

namespace Elsa.Studio.Labels.Client;

///  Represents a client API for managing secrets.
public interface ILabelsApi
{
    /// Lists all secrets.
    [Get("/labels")]
    Task<ListResponse<Label>> ListAsync(CancellationToken cancellationToken = default);
    
    /// Gets a secret by ID.
    [Get("/labels/{id}")]
    Task<Label> GetAsync(string id, CancellationToken cancellationToken = default);

    [Delete("/labels/{id}")]
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);

    [Post("/labels/{id}")]
    Task UpdateAsync(string id, LabelInputModel model, CancellationToken cancellationToken = default);

    [Post("/labels")]
    Task<Label> CreateAsync(LabelInputModel? inputModel, CancellationToken cancellationToken = default);
}