using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Environments.Contracts;

namespace Elsa.Studio.Environments.Services;

/// <summary>
/// An environment-aware backend connection provider that returns the URL to the currently selected environment, if any.
/// </summary>
public class EnvironmentBackendConnectionProvider : IBackendConnectionProvider
{
    private readonly IEnvironmentService _environmentService;
    private readonly IBackendAccessor _backendAccessor;

    public EnvironmentBackendConnectionProvider(IEnvironmentService environmentService, IBackendAccessor backendAccessor)
    {
        _environmentService = environmentService;
        _backendAccessor = backendAccessor;
    }
    
    public Uri Url => _environmentService.CurrentEnvironment?.Url ?? _backendAccessor.Backend.Url;
}