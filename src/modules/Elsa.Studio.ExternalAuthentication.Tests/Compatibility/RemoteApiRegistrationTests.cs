using Elsa.Api.Client.Resources.ExternalAuthentication.Connections.Contracts;
using Elsa.Api.Client.Resources.ExternalAuthentication.IdentityLinks.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.ExternalAuthentication.Extensions;
using Elsa.Studio.Models;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Compatibility;

public sealed class RemoteApiRegistrationTests
{
    [Fact]
    public void DefaultAndExternalAuthenticationRegistrations_CreateValidHandlerPipelines()
    {
        var services = new ServiceCollection();
        var backendApiConfig = new BackendApiConfig
        {
            ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(TestAuthenticationHandler)
        };

        services.AddRemoteBackend(backendApiConfig);
        services.AddExternalAuthenticationModule(backendApiConfig);

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<IHttpClientFactory>();

        using var connectionsClient = factory.CreateClient(nameof(IExternalAuthenticationConnectionsApi));
        using var linksClient = factory.CreateClient(nameof(IExternalIdentityLinksApi));

        Assert.NotNull(connectionsClient);
        Assert.NotNull(linksClient);
    }

    private sealed class TestAuthenticationHandler : DelegatingHandler;
}
