using Elsa.Studio.Contracts;

namespace Elsa.Studio.Authentication.Abstractions.Services;

/// <summary>
/// Default implementation of <see cref="IAuthenticationProviderManager"/> that queries registered <see cref="IAuthenticationProvider"/> instances.
/// </summary>
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

        return null;
    }
}

