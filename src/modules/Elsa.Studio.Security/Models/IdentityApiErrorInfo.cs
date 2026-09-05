namespace Elsa.Studio.Security.Models;

/// <summary>
/// Safe, display-ready information about a failed Identity request.
/// </summary>
public sealed record IdentityApiErrorInfo(
    string Code,
    string Message,
    bool IsAuthorization = false,
    bool IsNotFound = false,
    bool IsConflict = false,
    bool IsValidation = false)
{
    public static IdentityApiErrorInfo Unavailable { get; } =
        new("unavailable", "Role administration is unavailable right now. Try again in a moment.");
}
