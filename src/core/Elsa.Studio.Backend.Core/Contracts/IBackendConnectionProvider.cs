namespace Elsa.Studio.Backend.Contracts;

/// <summary>
/// Provides connection details to the backend.
/// </summary>
public interface IBackendConnectionProvider
{
    /// <summary>
    /// Gets the URL to the backend.
    /// </summary>
    Uri Url { get; }
}