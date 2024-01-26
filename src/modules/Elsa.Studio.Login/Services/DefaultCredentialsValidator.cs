using Elsa.Api.Client.Resources.Identity.Contracts;
using Elsa.Api.Client.Resources.Identity.Requests;
using Elsa.Studio.Contracts;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Refit;
using System.Net.Http;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// A default implementation of <see cref="ICredentialsValidator"/> that consumes the endpoints from Elsa.Identity.
/// </summary>
public class DefaultCredentialsValidator : ICredentialsValidator
{
    private readonly IRemoteBackendApiClientProvider _remoteBackendApiClientProvider;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCredentialsValidator"/> class.
    /// </summary>
    public DefaultCredentialsValidator(IRemoteBackendApiClientProvider remoteBackendApiClientProvider, IHttpClientFactory httpClientFactory)
    {
        _remoteBackendApiClientProvider = remoteBackendApiClientProvider;
        _httpClientFactory = httpClientFactory;

    }

    /// <inheritdoc />
    public async ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var api = await _remoteBackendApiClientProvider.GetApiAsync<ILoginApi>(cancellationToken);

        var request = new LoginRequest(username, password);
        var response = await api.LoginAsync(request, cancellationToken);

        return new ValidateCredentialsResult(response.IsAuthenticated, response.AccessToken, response.RefreshToken);
    }
}