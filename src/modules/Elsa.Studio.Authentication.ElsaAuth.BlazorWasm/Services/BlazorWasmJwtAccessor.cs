using Blazored.LocalStorage;
using Elsa.Studio.Authentication.ElsaAuth.Services;

namespace Elsa.Studio.Authentication.ElsaAuth.BlazorWasm.Services;

/// <summary>
/// Blazor WebAssembly implementation of JWT accessor.
/// </summary>
public class BlazorWasmJwtAccessor(ILocalStorageService localStorageService) : JwtAccessorBase(localStorageService);
