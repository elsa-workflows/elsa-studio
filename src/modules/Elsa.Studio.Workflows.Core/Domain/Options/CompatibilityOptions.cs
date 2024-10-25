namespace Elsa.Studio.Workflows.Domain.Options;

/// <summary>
/// Options for compatibility.
/// </summary>
public class CompatibilityOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to require a schema for importing workflows.
    /// </summary>
    public bool RequireSchemaForImport { get; set; }
}