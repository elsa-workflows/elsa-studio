namespace Elsa.Studio.Authentication.ElsaAuth.Models;

/// <summary>
/// Result of validating credentials.
/// </summary>
public record ValidateCredentialsResult(bool IsValid, string? AccessToken, string? RefreshToken);

