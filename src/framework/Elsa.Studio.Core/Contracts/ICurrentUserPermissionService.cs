namespace Elsa.Studio.Contracts;

/// <summary>
/// Reads the current user's permission claims for client-side affordance checks.
/// Server-side authorization remains the source of truth.
/// </summary>
public interface ICurrentUserPermissionService
{
    /// <summary>Returns whether the current user holds the required permission.</summary>
    ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default);

    /// <summary>Returns the current user's permission grants.</summary>
    ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default);
}
