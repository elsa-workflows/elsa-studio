using Blazored.LocalStorage;
using Elsa.Studio.Authentication.JwtBearer.Contracts;
using Microsoft.AspNetCore.Http;

namespace Elsa.Studio.Login.BlazorServer.Services;

/// <summary>
/// Implements the <see cref="IJwtAccessor"/> interface for server-side Blazor.
/// </summary>
public class BlazorServerJwtAccessor : IJwtAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILocalStorageService _localStorageService;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorServerJwtAccessor"/> class.
    /// </summary>
    public BlazorServerJwtAccessor(IHttpContextAccessor httpContextAccessor, ILocalStorageService localStorageService)
    {
        _httpContextAccessor = httpContextAccessor;
        _localStorageService = localStorageService;
    }

    /// <inheritdoc />
    public async ValueTask<string> ReadTokenAsync(string name)
    {
        if (IsPrerendering())
            return null;
        
        return await _localStorageService.GetItemAsync<string>(name);
    }

    /// <inheritdoc />
    public async ValueTask WriteTokenAsync(string name, string token)
    {
        await _localStorageService.SetItemAsStringAsync(name, token);
    }

    private bool IsPrerendering() => _httpContextAccessor.HttpContext?.Response.HasStarted == false;
}