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
}
