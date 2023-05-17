namespace Elsa.Studio.Login.Models;

public record ValidateCredentialsResult(bool IsAuthenticated, string? AccessToken, string? RefreshToken);