namespace Elsa.Studio.Authentication.ElsaAuth.Contracts;

/// <summary>
/// Performs authentication redirects and receives authorization codes when applicable.
/// </summary>
public interface IAuthorizationService
{
    /// <summary>
    /// Redirects to the authorization server or login page.
    /// </summary>
    Task RedirectToAuthorizationServer();

    /// <summary>
    /// Receives an authorization code (used by legacy in-app OIDC code flow).
    /// </summary>
    Task ReceiveAuthorizationCode(string code, string? state, CancellationToken cancellationToken);
}

