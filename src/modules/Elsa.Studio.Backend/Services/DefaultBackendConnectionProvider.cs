using Elsa.Studio.Backend.Contracts;

namespace Elsa.Studio.Backend.Services;

/// <summary>
/// A default implementation of <see cref="IBackendConnectionProvider"/>.
/// </summary>
public class DefaultBackendConnectionProvider : IBackendConnectionProvider
{
    private readonly IBackendAccessor _backendAccessor;

    public DefaultBackendConnectionProvider(IBackendAccessor backendAccessor)
    {
        _backendAccessor = backendAccessor;
    }
    
    public Uri Url => _backendAccessor.Backend.Url;
}