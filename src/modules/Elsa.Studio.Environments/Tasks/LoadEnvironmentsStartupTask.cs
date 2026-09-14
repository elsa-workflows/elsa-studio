using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Refit;

namespace Elsa.Studio.Environments.Tasks;

/// <summary>
/// Loads available environments from the current backend into <see cref="IEnvironmentService"/>.
/// </summary>
public class LoadEnvironmentsStartupTask(
    IBackendApiClientProvider backendApiClientProvider,
    IEnvironmentService environmentService) : IStartupTask
{
    /// <summary>
    /// Fetches environments through <see cref="IBackendApiClientProvider"/> and stores them.
    /// </summary>
    public async ValueTask LoadAsync(CancellationToken cancellationToken = default)
    {
        var environmentsClient = await backendApiClientProvider.GetApiAsync<IEnvironmentsClient>(cancellationToken);
        var response = await environmentsClient.ListEnvironmentsAsync(cancellationToken);
        environmentService.SetEnvironments(response.Environments, response.DefaultEnvironmentName);
    }

    /// <inheritdoc />
    public async ValueTask ExecuteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await LoadAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Hosted-service startup can run before the user is authenticated.
        }
        catch (ApiException)
        {
            // Same: a 401/404 at host start must not take down Blazor Server.
        }
    }
}
