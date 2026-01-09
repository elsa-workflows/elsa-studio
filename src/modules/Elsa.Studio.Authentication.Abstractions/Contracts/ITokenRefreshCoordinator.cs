namespace Elsa.Studio.Authentication.Abstractions.Contracts;

/// <summary>
/// Coordinates token refresh operations to prevent multiple concurrent refresh attempts.
/// </summary>
public interface ITokenRefreshCoordinator
{
    /// <summary>
    /// Ensures only one refresh operation runs at a time for the current scope.
    /// </summary>
    Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken);
}

