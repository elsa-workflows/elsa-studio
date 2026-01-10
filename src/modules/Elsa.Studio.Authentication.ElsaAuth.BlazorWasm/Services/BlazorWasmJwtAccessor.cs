using Blazored.LocalStorage;
using Elsa.Studio.Authentication.ElsaAuth.Services;

namespace Elsa.Studio.Authentication.ElsaAuth.BlazorWasm.Services;

/// <summary>
/// Blazor WebAssembly implementation of JWT accessor.
/// </summary>
public class BlazorWasmJwtAccessor : JwtAccessorBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BlazorWasmJwtAccessor"/> class.
    /// </summary>
    public BlazorWasmJwtAccessor(ILocalStorageService localStorageService) : base(localStorageService)
    {
    }
}
