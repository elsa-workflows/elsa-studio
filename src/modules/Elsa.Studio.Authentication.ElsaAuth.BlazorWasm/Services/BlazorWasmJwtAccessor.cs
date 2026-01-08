using Blazored.LocalStorage;
using Elsa.Studio.Authentication.ElsaAuth.Contracts;

namespace Elsa.Studio.Authentication.ElsaAuth.BlazorWasm.Services;

/// <inheritdoc />
public class BlazorWasmJwtAccessor : IJwtAccessor
{
    private readonly ILocalStorageService _localStorageService;

    public BlazorWasmJwtAccessor(ILocalStorageService localStorageService) => _localStorageService = localStorageService;

    /// <inheritdoc />
    public async ValueTask<string?> ReadTokenAsync(string name) => await _localStorageService.GetItemAsync<string>(name);

    /// <inheritdoc />
    public async ValueTask WriteTokenAsync(string name, string token) => await _localStorageService.SetItemAsStringAsync(name, token);

    /// <inheritdoc />
    public async ValueTask ClearTokenAsync(string name) => await _localStorageService.RemoveItemAsync(name);
}
