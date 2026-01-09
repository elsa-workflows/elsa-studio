namespace Elsa.Studio.Authentication.Abstractions.Models;

/// <summary>
/// Configuration options for token purposes, allowing different scopes for different use cases.
/// </summary>
public sealed class TokenPurposeOptions
{
    /// <summary>
    /// Maps purpose names to their required scopes.
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
