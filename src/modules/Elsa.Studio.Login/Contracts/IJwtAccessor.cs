namespace Elsa.Studio.Authentication.JwtBearer.Contracts;

/// <summary>
/// Reads the token from storage (e.g. cookie, local storage, etc.).
/// </summary>
public interface IJwtAccessor
{
    ValueTask<string> ReadTokenAsync(string name);
    ValueTask WriteTokenAsync(string name, string token);
}