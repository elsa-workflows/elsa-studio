using Blazored.LocalStorage;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Core.BlazorWasm.Services;

public class ClientJwtAccessor : IJwtAccessor
{
    private readonly ILocalStorageService _localStorageService;

    public ClientJwtAccessor(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }
    
    public async ValueTask<string?> ReadTokenAsync(string name)
    {   
        return await _localStorageService.GetItemAsync<string>(name);
    }

    public async ValueTask WriteTokenAsync(string name, string token)
    {
        await _localStorageService.SetItemAsStringAsync(name, token);
    }
}