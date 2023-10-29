namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides connection details to the backend.
/// </summary>
public interface IRemoteBackendApiClientProvider
{
    /// <summary>
    /// Gets the URL to the backend.
    /// </summary>
    Uri Url { get; }

    /// <summary>
    /// Gets an API client from the backend connection provider.
    /// </summary>
    /// <typeparam name="T">The API client type.</typeparam>
    /// <returns>The API client.</returns>
    ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class;
}