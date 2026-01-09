using Elsa.Studio.Authentication.Abstractions.Contracts;

namespace Elsa.Studio.Authentication.Abstractions.Services;

/// <summary>
/// Default implementation of <see cref="ITokenRefreshCoordinator"/>.
/// </summary>
public class TokenRefreshCoordinator : ITokenRefreshCoordinator
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    /// <inheritdoc />
    public async Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);

        try
        {
            return await action(cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

