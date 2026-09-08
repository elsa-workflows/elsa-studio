namespace Elsa.Studio.Contracts;

/// <summary>
/// Loads effective permissions for the current user when the authentication principal does not carry Elsa permission claims.
/// </summary>
public interface ICurrentUserPermissionSource
{
    /// <summary>
    /// Returns effective permission grants, or <see langword="null"/> when they cannot be loaded.
    /// </summary>
    ValueTask<IReadOnlySet<string>?> GetAsync(CancellationToken cancellationToken = default);
}
