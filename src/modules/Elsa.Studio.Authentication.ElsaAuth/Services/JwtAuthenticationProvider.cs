using Elsa.Studio.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.Contracts;

namespace Elsa.Studio.Authentication.ElsaAuth.Services;

/// <inheritdoc />
public class JwtAuthenticationProvider(IJwtAccessor jwtAccessor) : IAuthenticationProvider
{
    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(string tokenName, CancellationToken cancellationToken = default) => await jwtAccessor.ReadTokenAsync(tokenName);
}

