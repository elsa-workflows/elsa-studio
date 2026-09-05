using Elsa.Studio.Security.Models;
using Refit;

namespace Elsa.Studio.Security.Client;

public interface IRolesApi
{
    [Get("/identity/roles")]
    Task<ListRolesResponse> ListAsync(CancellationToken cancellationToken = default);

    [Post("/identity/roles")]
    Task<RoleSummary> CreateAsync([Body] CreateRoleRequest request, CancellationToken cancellationToken = default);

    [Put("/identity/roles/{id}")]
    Task<RoleSummary> UpdateAsync(string id, [Body] UpdateRoleRequest request, CancellationToken cancellationToken = default);

    [Delete("/identity/roles/{id}")]
    Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
