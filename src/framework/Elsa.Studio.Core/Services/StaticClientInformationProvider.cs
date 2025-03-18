using Elsa.Studio.Contracts;
using Elsa.Studio.Models;

namespace Elsa.Studio.Services;

/// <summary>
/// 
/// </summary>
public class StaticClientInformationProvider : IClientInformationProvider
{
    /// <inheritdoc />
    public ValueTask<ClientInformation> GetInfoAsync(CancellationToken cancellationToken = default)
    {
        var version = ToolVersion.Version.ToString();
        return new(new ClientInformation(version));
    }
}