using Blazored.LocalStorage;
using Elsa.Studio.Authentication.ElsaAuth.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.Studio.Authentication.ElsaAuth.BlazorServer.Services;

/// <summary>
/// Blazor Server implementation of JWT accessor with prerendering support.
/// </summary>
public class BlazorServerJwtAccessor : JwtAccessorBase
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorServerJwtAccessor"/> class.
    /// </summary>
    public BlazorServerJwtAccessor(IHttpContextAccessor httpContextAccessor, ILocalStorageService localStorageService)
        : base(localStorageService)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    protected override bool CanAccessStorage() => !IsPrerendering();

    private bool IsPrerendering() => _httpContextAccessor.HttpContext?.Response.HasStarted == false;
}
