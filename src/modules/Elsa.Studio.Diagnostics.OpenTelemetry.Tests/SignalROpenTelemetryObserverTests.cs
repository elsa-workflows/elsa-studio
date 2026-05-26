using System.Diagnostics.CodeAnalysis;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Elsa.Studio.Diagnostics.OpenTelemetry.Services;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Tests;

public class SignalROpenTelemetryObserverTests
{
    [Fact]
    public async Task ObserveAsync_WhenConnectionFails_CompletesWithoutItems()
    {
        var observer = CreateObserver();
        var items = new List<OpenTelemetryStreamItem>();

        await foreach (var item in observer.ObserveAsync(new OpenTelemetryTraceFilter { TraceId = "trace-1" }))
            items.Add(item);

        Assert.Empty(items);
        Assert.Equal("trace-1", observer.CurrentFilter?.TraceId);
        await observer.DisposeAsync();
    }

    [Fact]
    public async Task ObserveAsync_WhenStartedAgain_ReplacesCurrentFilter()
    {
        var observer = CreateObserver();

        await foreach (var _ in observer.ObserveAsync(new OpenTelemetryTraceFilter { TraceId = "trace-1" }))
        {
        }

        await foreach (var _ in observer.ObserveAsync(new OpenTelemetryTraceFilter { TraceId = "trace-2" }))
        {
        }

        Assert.Equal("trace-2", observer.CurrentFilter?.TraceId);
        await observer.DisposeAsync();
    }

    private static SignalROpenTelemetryObserver CreateObserver()
    {
        return new SignalROpenTelemetryObserver(
            new TestBackendApiClientProvider(new("http://127.0.0.1:1/")),
            new NoopConnectionOptionsConfigurator(),
            NullLogger<SignalROpenTelemetryObserver>.Instance);
    }

    private class TestBackendApiClientProvider(Uri url) : IBackendApiClientProvider
    {
        public Uri Url { get; } = url;

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class => throw new NotSupportedException();
    }

    private class NoopConnectionOptionsConfigurator : IHttpConnectionOptionsConfigurator
    {
        public Task ConfigureAsync(HttpConnectionOptions options, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
