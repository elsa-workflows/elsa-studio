namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides logout functionality for the current authentication provider.
/// </summary>
public interface ILogoutService
{
    /// <summary>
    /// Performs logout for the current authentication provider.
    /// </summary>
    /// <returns>A task representing the asynchronous logout operation.</returns>
    Task LogoutAsync();
}