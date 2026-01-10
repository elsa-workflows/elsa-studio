using Elsa.Studio.Contracts;

namespace Elsa.Studio.Login.Services;

/// <inheritdoc />
public class DefaultAuthenticationProviderManager(IEnumerable<IAuthenticationProvider> authenticationProviders) : IAuthenticationProviderManager
{
    /// <inheritdoc />
    public async Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        foreach (var authenticationProvider in authenticationProviders)
        {
            var token = await authenticationProvider.GetAccessTokenAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(token))
                return token;
        }

        return string.Empty;
    }
}