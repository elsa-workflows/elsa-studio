using Elsa.Studio.Login.Models;

namespace Elsa.Studio.Login.Contracts;

/// <summary>
/// A validator for credentials.
/// </summary>
public interface ICredentialsValidator
{
    ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default);
}