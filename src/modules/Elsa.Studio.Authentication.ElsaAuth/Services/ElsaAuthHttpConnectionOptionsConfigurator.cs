using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Authentication.ElsaAuth.Contracts;
using Microsoft.AspNetCore.Http.Connections.Client;

namespace Elsa.Studio.Authentication.ElsaAuth.Services;

/// <summary>
/// ElsaAuth implementation of <see cref="IHttpConnectionOptionsConfigurator"/> that configures SignalR connections
/// to use JWT access tokens.
/// </summary>
public class ElsaAuthHttpConnectionOptionsConfigurator(IAuthenticationProvider authenticationProvider) : IHttpConnectionOptionsConfigurator
{
    /// <inheritdoc />
    public Task ConfigureAsync(HttpConnectionOptions connectionOptions, CancellationToken cancellationToken = default)
    {
        // Configure access token provider for SignalR connection
        connectionOptions.AccessTokenProvider = async () =>
        {
            var token = await authenticationProvider.GetAccessTokenAsync(cancellationToken);
            return token ?? string.Empty;
        };

        return Task.CompletedTask;
    }
}
