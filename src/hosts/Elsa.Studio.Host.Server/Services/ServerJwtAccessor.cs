using Blazored.LocalStorage;
using Elsa.Studio.Contracts;

namespace Elsa.Studio.Host.Server.Services;

public class ServerJwtAccessor : IJwtAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILocalStorageService _localStorageService;

    public ServerJwtAccessor(IHttpContextAccessor httpContextAccessor, ILocalStorageService localStorageService)
    {
        _httpContextAccessor = httpContextAccessor;
        _localStorageService = localStorageService;
    }
    
    public async ValueTask<string?> ReadTokenAsync()
    {
        if (IsPrerendering())
            return null;
        
        return await _localStorageService.GetItemAsync<string>("authToken");
    }

    private bool IsPrerendering() => _httpContextAccessor.HttpContext?.Response.HasStarted == false;
}