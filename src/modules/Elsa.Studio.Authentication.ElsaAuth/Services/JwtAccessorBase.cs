using Blazored.LocalStorage;
using Elsa.Studio.Authentication.ElsaAuth.Contracts;

namespace Elsa.Studio.Authentication.ElsaAuth.Services;

/// <summary>
/// Abstract base class for JWT token accessor implementations that use local storage.
/// Provides common functionality for reading, writing, and clearing tokens.
/// </summary>
public abstract class JwtAccessorBase : IJwtAccessor
{
    private readonly ILocalStorageService _localStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtAccessorBase"/> class.
    /// </summary>
    /// <param name="localStorageService">The local storage service.</param>
    protected JwtAccessorBase(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }

    /// <summary>
    /// Determines whether storage operations can be performed.
    /// Override this method to implement platform-specific checks (e.g., prerendering detection).
    /// </summary>
    /// <returns>True if storage can be accessed; otherwise, false.</returns>
    protected virtual bool CanAccessStorage() => true;

    /// <inheritdoc />
    public virtual async ValueTask<string?> ReadTokenAsync(string name)
    {
        if (!CanAccessStorage())
            return null;

        return await _localStorageService.GetItemAsync<string>(name);
    }

    /// <inheritdoc />
    public virtual async ValueTask WriteTokenAsync(string name, string token)
    {
        if (!CanAccessStorage())
            return;

        await _localStorageService.SetItemAsStringAsync(name, token);
    }

    /// <inheritdoc />
    public virtual async ValueTask ClearTokenAsync(string name)
    {
        if (!CanAccessStorage())
            return;

        await _localStorageService.RemoveItemAsync(name);
    }
}
