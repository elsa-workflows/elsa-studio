using Blazored.LocalStorage;
using Elsa.Studio.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Studio.Core.BlazorServer.Services;

public class ServerJwtAccessor : IJwtAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILocalStorageService _localStorageService;

    public ServerJwtAccessor(IHttpContextAccessor httpContextAccessor, ILocalStorageService localStorageService)
    {
        _httpContextAccessor = httpContextAccessor;
        _localStorageService = localStorageService;
    }
    
    public async ValueTask<string?> ReadTokenAsync(string name)
    {
        if (IsPrerendering())
            return null;
        
        return await _localStorageService.GetItemAsync<string>(name);
    }

    public async ValueTask WriteTokenAsync(string name, string token)
    {
        await _localStorageService.SetItemAsStringAsync(name, token);
    }

    private bool IsPrerendering() => _httpContextAccessor.HttpContext?.Response.HasStarted == false;
}