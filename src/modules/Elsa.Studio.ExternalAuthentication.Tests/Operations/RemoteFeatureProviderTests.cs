using Elsa.Api.Client.Resources.Features.Contracts;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Operations;

public class RemoteFeatureProviderTests
{
    [Fact]
    public async Task FeatureChecks_ReuseTheInstalledFeatureCatalog()
    {
        var api = new FeaturesApi();
        var provider = new RemoteFeatureProvider(new BackendApiClientProvider(api));

        Assert.True(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.False(await provider.IsEnabledAsync("Elsa.AI"));
        Assert.Equal(1, api.ListCalls);
        Assert.Equal(0, api.GetCalls);
    }

    [Fact]
    public async Task AnonymousFeatureChecks_DoNotProbeTheProtectedBackend()
    {
        var api = new FeaturesApi();
        var authentication = new AuthenticationStateProviderStub();
        var provider = new RemoteFeatureProvider(new BackendApiClientProvider(api), authentication);

        Assert.False(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.Equal(0, api.ListCalls);

        authentication.IsAuthenticated = true;
        Assert.True(await provider.IsEnabledAsync("Elsa.ExternalAuthentication"));
        Assert.Equal(1, api.ListCalls);
    }

    private sealed class BackendApiClientProvider(IFeaturesApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://elsa.example.test/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            ValueTask.FromResult((T)api);
    }

    private sealed class FeaturesApi : IFeaturesApi
    {
        public int GetCalls { get; private set; }
        public int ListCalls { get; private set; }

        public Task<FeatureDescriptor> GetAsync(string fullName, CancellationToken cancellationToken = default)
        {
            GetCalls++;
            throw new NotSupportedException();
        }

        public Task<ListResponse<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default)
        {
            ListCalls++;
            return Task.FromResult(new ListResponse<FeatureDescriptor>(
                [new FeatureDescriptor { FullName = "Elsa.ExternalAuthentication" }],
                1));
        }
    }

    private sealed class AuthenticationStateProviderStub : AuthenticationStateProvider
    {
        public bool IsAuthenticated { get; set; }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var identity = IsAuthenticated
                ? new ClaimsIdentity([new Claim(ClaimTypes.Name, "admin")], "test")
                : new ClaimsIdentity();
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
        }
    }
}
