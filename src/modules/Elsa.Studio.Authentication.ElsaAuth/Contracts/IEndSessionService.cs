namespace Elsa.Studio.Authentication.ElsaAuth.Contracts;

/// <summary>
/// Ends the current session (logout).
/// </summary>
public interface IEndSessionService
{
    /// <summary>
    /// Signs out.
    /// </summary>
    Task EndSessionAsync(CancellationToken cancellationToken = default);
}

