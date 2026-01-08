namespace Elsa.Studio.Authentication.ElsaAuth.Contracts;

/// <summary>
/// Reads and writes tokens to storage (e.g. cookies, local storage, etc.).
/// </summary>
public interface IJwtAccessor
{
    /// <summary>
    /// Reads a token by name.
    /// </summary>
    ValueTask<string?> ReadTokenAsync(string name);

    /// <summary>
    /// Writes a token by name.
    /// </summary>
    ValueTask WriteTokenAsync(string name, string token);
}
