namespace Elsa.Studio.Authentication.Abstractions.Models;

/// <summary>
/// Configuration options for backend API scopes.
/// Used in OIDC multi-audience scenarios where the backend API requires different scopes than the default sign-in scopes.
/// </summary>
public sealed class BackendApiScopeOptions
{
    /// <summary>
    /// The scopes to request when calling the backend API.
    /// </summary>
    /// <example>
    /// ["api://my-api/elsa-server-api"]
    /// </example>
    public string[] Scopes { get; set; } = Array.Empty<string>();
}
