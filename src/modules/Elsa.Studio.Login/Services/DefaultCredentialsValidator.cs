using Elsa.Api.Client.Resources.Identity.Contracts;
using Elsa.Api.Client.Resources.Identity.Requests;
using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Refit;

namespace Elsa.Studio.Login.Services;

/// <summary>
/// A default implementation of <see cref="ICredentialsValidator"/> that consumes the endpoints from Elsa.Identity.
/// </summary>
public class DefaultCredentialsValidator : ICredentialsValidator
{
    private readonly IBackendConnectionProvider _backendConnectionProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultCredentialsValidator"/> class.
    /// </summary>
    public DefaultCredentialsValidator(IBackendConnectionProvider backendConnectionProvider)
    {
        _backendConnectionProvider = backendConnectionProvider;
    }

    /// <inheritdoc />
    public async ValueTask<ValidateCredentialsResult> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var serverUrl = _backendConnectionProvider.Url.ToString();
        var api = RestService.For<ILoginApi>(serverUrl);
        var request = new LoginRequest(username, password);
        var response = await api.LoginAsync(request, cancellationToken);

        return new ValidateCredentialsResult(response.IsAuthenticated, response.AccessToken, response.RefreshToken);
    }
}