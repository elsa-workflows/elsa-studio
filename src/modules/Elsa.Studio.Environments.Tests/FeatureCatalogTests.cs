using Elsa.Api.Client.Resources.Features.Contracts;
using Elsa.Studio.Attributes;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Environments.Tests;

/// <summary>
/// The default environment must be selected before <see cref="IRemoteFeatureProvider.ListAsync"/>
/// so features are gated against that environment's catalog, not the fallback backend.
/// </summary>
public class FeatureCatalogTests : IDisposable
{
    private readonly EnvironmentsModuleTests.RecordingHandler _handler = new();
    private readonly DefaultOnlyFeature _defaultOnlyFeature = new();
    private readonly FallbackOnlyFeature _fallbackOnlyFeature = new();
    private readonly ServiceProvider _serviceProvider;

    public FeatureCatalogTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreInternal();

        var backendApiConfig = new BackendApiConfig
        {
            ConfigureBackendOptions = options => options.Url = new Uri("https://backend.example/"),
            ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(PassthroughAuthenticationHandler)
        };

        services.AddRemoteBackend(backendApiConfig);
        services.AddEnvironmentsModule(backendApiConfig);
        // Same order as Host.Server: Workflows registers IRemoteFeatureProvider after Environments.
        services.AddScoped<IRemoteFeatureProvider, RemoteFeatureProvider>();
        services.AddSingleton<IFeature>(_defaultOnlyFeature);
        services.AddSingleton<IFeature>(_fallbackOnlyFeature);
        services.AddHttpClient(nameof(IEnvironmentsClient)).ConfigurePrimaryHttpMessageHandler(() => _handler);
        services.AddHttpClient(nameof(IFeaturesApi)).ConfigurePrimaryHttpMessageHandler(() => _handler);

        _handler.ResponseContentForRequest = request =>
        {
            var url = request.RequestUri?.ToString() ?? string.Empty;
            if (url.Contains("/environments", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    {
                      "environments": [
                        { "name": "Fallback", "url": "https://backend.example/" },
                        { "name": "Dev", "url": "https://dev.example/" }
                      ],
                      "defaultEnvironmentName": "Dev"
                    }
                    """;
            }

            if (url.Contains("dev.example", StringComparison.OrdinalIgnoreCase))
            {
                return """
                    {
                      "items": [
                        { "fullName": "Elsa.DefaultOnly", "name": "DefaultOnly", "namespace": "Elsa" }
                      ],
                      "totalCount": 1
                    }
                    """;
            }

            return """
                {
                  "items": [
                    { "fullName": "Elsa.FallbackOnly", "name": "FallbackOnly", "namespace": "Elsa" }
                  ],
                  "totalCount": 1
                }
                """;
        };

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public async Task InitializeFeatures_UsesTheDefaultEnvironmentCatalog()
    {
        // Arrange
        var features = _serviceProvider.GetRequiredService<IFeatureService>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act
        await features.InitializeFeaturesAsync();

        // Assert
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
        Assert.Equal(new Uri("https://dev.example/"), environments.CurrentEnvironment?.Url);
        Assert.True(_defaultOnlyFeature.Initialized);
        Assert.False(_fallbackOnlyFeature.Initialized);
        Assert.Contains(_handler.Requests, request =>
            request.RequestUri is not null
            && request.RequestUri.Host == "dev.example"
            && request.RequestUri.AbsolutePath.Contains("features", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(_handler.Requests, request =>
            request.RequestUri is not null
            && request.RequestUri.Host == "backend.example"
            && request.RequestUri.AbsolutePath.Contains("features", StringComparison.OrdinalIgnoreCase));
    }

    [RemoteFeature("Elsa.DefaultOnly")]
    private sealed class DefaultOnlyFeature : IFeature
    {
        public bool Initialized { get; private set; }

        public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            Initialized = true;
            return ValueTask.CompletedTask;
        }
    }

    [RemoteFeature("Elsa.FallbackOnly")]
    private sealed class FallbackOnlyFeature : IFeature
    {
        public bool Initialized { get; private set; }

        public ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            Initialized = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class PassthroughAuthenticationHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            base.SendAsync(request, cancellationToken);
    }
}
