using Blazored.LocalStorage;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Host.Wasm.Services;

public class ClientJwtAccessor : IJwtAccessor
{
    private readonly ILocalStorageService _localStorageService;

    public ClientJwtAccessor(ILocalStorageService localStorageService)
    {
        _localStorageService = localStorageService;
    }
    
    public async ValueTask<string?> ReadTokenAsync()
    {   
        return await _localStorageService.GetItemAsync<string>("authToken");
    }
}