namespace Elsa.Studio.Login.Models;

/// <summary>
/// Represents the result of validate credentials.
/// </summary>
public record ValidateCredentialsResult(bool IsAuthenticated, string? AccessToken, string? RefreshToken);