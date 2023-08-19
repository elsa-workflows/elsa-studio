using Elsa.Studio.Backend.Models;

namespace Elsa.Studio.Backend.Contracts;

/// <summary>
/// Provides access to the current remote backend.
/// </summary>
public interface IRemoteBackendAccessor
{
    /// <summary>
    /// Gets or sets the current backend.
    /// </summary>
    RemoteBackend RemoteBackend { get; }
}