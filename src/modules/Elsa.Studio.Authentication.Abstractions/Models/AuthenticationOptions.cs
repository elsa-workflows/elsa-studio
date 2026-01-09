namespace Elsa.Studio.Authentication.Abstractions.Models;

/// <summary>
/// Base configuration options for authentication providers.
/// Authentication providers can extend this class to add provider-specific options.
/// </summary>
public abstract class AuthenticationOptions
{
    /// <summary>
    /// Gets or sets the scopes to request.
    /// </summary>
    public string[] Scopes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets whether to require HTTPS for metadata endpoints.
    /// </summary>
    public bool RequireHttpsMetadata { get; set; } = true;

    /// <summary>
    /// Maps purpose names to their required scopes.
    /// Allows different scopes for different use cases (e.g., backend API vs. Graph API).
    /// </summary>
    /// <example>
    /// {
    ///   "backend_api": ["api://my-api/scope"],
    ///   "graph": ["https://graph.microsoft.com/User.Read"]
    /// }
    /// </example>
    public Dictionary<string, string[]> ScopesByPurpose { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Which purpose the API handler should use by default when calling backend APIs.
    /// </summary>
    public string BackendApiPurpose { get; set; } = "backend_api";
}
