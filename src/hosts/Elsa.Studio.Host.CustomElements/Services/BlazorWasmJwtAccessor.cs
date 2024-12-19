using Elsa.Studio.Login.Contracts;

namespace Elsa.Studio.Host.CustomElements.Services;

/// <inheritdoc />
public class BlazorWasmJwtAccessor : IJwtAccessor
{
    private readonly BackendService _backendService;

    /// <summary>
    /// Provides access to JWT tokens using a backend service.
    /// </summary>
    /// <param name="backendService"></param>
    public BlazorWasmJwtAccessor(BackendService backendService)
    {
        _backendService = backendService;
    }

    /// <inheritdoc />
    public ValueTask<string?> ReadTokenAsync(string name)
    {   
        return ValueTask.FromResult(_backendService.AccessToken);
    }

    /// <inheritdoc />
    public ValueTask WriteTokenAsync(string name, string token)
    {
        if (name == "AccessToken")
            _backendService.AccessToken = token;
        
        return new ValueTask();
    }
}