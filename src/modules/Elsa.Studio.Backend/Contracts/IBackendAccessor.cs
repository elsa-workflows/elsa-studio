namespace Elsa.Studio.Backend.Contracts;

/// <summary>
/// Provides access to the current backend.
/// </summary>
public interface IBackendAccessor
{
    /// <summary>
    /// Gets or sets the current backend.
    /// </summary>
    Models.Backend Backend { get; }
}