namespace Elsa.Studio.Contracts;

/// <summary>
/// Defines a manager responsible for handling authentication providers and retrieving authentication tokens.
/// </summary>
public interface IAuthenticationProviderManager
{
    /// <summary>
    /// Asynchronously retrieves an access token using the available authentication providers.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous operation. The task result contains the access token as a string, or null if no valid token is available.</returns>
    Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}