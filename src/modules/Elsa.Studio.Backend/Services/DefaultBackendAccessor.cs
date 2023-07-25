using Elsa.Studio.Backend.Contracts;
using Elsa.Studio.Backend.Options;
using Microsoft.Extensions.Options;

namespace Elsa.Studio.Backend.Services;

/// <summary>
/// A default implementation of <see cref="IBackendAccessor"/>.
/// </summary>
public class DefaultBackendAccessor : IBackendAccessor
{
    private readonly IOptions<BackendOptions> _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultBackendAccessor"/> class.
    /// </summary>
    public DefaultBackendAccessor(IOptions<BackendOptions> options)
    {
        _options = options;
    }

    /// <inheritdoc />
    public Models.Backend Backend => new(_options.Value.Url);
}