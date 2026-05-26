using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Contracts;
using Elsa.Studio.Diagnostics.OpenTelemetry.Models;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.Diagnostics.OpenTelemetry.Services;

/// <summary>
/// SignalR-backed observer for live OpenTelemetry diagnostics updates.
/// </summary>
public class SignalROpenTelemetryObserver(
    IBackendApiClientProvider backendApiClientProvider,
    IHttpConnectionOptionsConfigurator httpConnectionOptionsConfigurator,
    ILogger<SignalROpenTelemetryObserver> logger) : IOpenTelemetryObserver
{
    private HubConnection? _connection;

    public OpenTelemetryTraceFilter? CurrentFilter { get; private set; }

    public async IAsyncEnumerable<OpenTelemetryStreamItem> ObserveAsync(OpenTelemetryTraceFilter filter, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CurrentFilter = filter;
        var channel = Channel.CreateUnbounded<OpenTelemetryStreamItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        await DisposeConnectionAsync();
        _connection = await CreateConnectionAsync(channel, cancellationToken);

        try
        {
            await _connection.StartAsync(cancellationToken);
            await _connection.SendAsync("SubscribeAsync", CurrentFilter, cancellationToken);
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.NotFound)
        {
            logger.LogWarning("The diagnostics OpenTelemetry hub was not found. Make sure the backend maps the OpenTelemetry hub.");
            channel.Writer.TryComplete();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogWarning(e, "Failed to connect to the diagnostics OpenTelemetry hub.");
            channel.Writer.TryComplete();
        }

        await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            yield return item;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
    }

    private async Task<HubConnection> CreateConnectionAsync(Channel<OpenTelemetryStreamItem> channel, CancellationToken cancellationToken)
    {
        var hubUrl = new Uri(backendApiClientProvider.Url, "hubs/diagnostics/opentelemetry").ToString();
        HttpConnectionOptions? capturedOptions = null;

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => capturedOptions = options)
            .WithAutomaticReconnect()
            .Build();

        if (capturedOptions != null)
            await httpConnectionOptionsConfigurator.ConfigureAsync(capturedOptions, cancellationToken);

        connection.On<OpenTelemetryStreamItem>("ReceiveAsync", item => channel.Writer.TryWrite(item));
        connection.Reconnected += async _ =>
        {
            if (CurrentFilter != null)
                await connection.SendAsync("SubscribeAsync", CurrentFilter, CancellationToken.None);
        };
        connection.Closed += exception =>
        {
            channel.Writer.TryComplete(exception);
            return Task.CompletedTask;
        };

        return connection;
    }

    private async Task DisposeConnectionAsync()
    {
        if (_connection == null)
            return;

        try
        {
            await _connection.StopAsync();
        }
        finally
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
