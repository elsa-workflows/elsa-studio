namespace Elsa.Studio.Contracts;

/// <summary>
/// Reads the token from storage (e.g. cookie, local storage, etc.).
/// </summary>
public interface IJwtAccessor
{
    ValueTask<string?> ReadTokenAsync();
}