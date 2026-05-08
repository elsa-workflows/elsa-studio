using System.Net;
using Elsa.Studio.Authentication.Abstractions.Contracts;
using Elsa.Studio.Contracts;
using Elsa.Studio.ServerLogs.Contracts;
using Elsa.Studio.ServerLogs.Models;
using Microsoft.AspNetCore.Http.Connections.Client;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Elsa.Studio.ServerLogs.Services;

/// <summary>
/// SignalR-backed observer for live server log events.
/// </summary>
public class SignalRServerLogObserver(
    IBackendApiClientProvider backendApiClientProvider,
    IHttpConnectionOptionsConfigurator httpConnectionOptionsConfigurator,
    ILogger<SignalRServerLogObserver> logger) : IServerLogObserver
{
    private HubConnection? _connection;
    private ServerLogFilter? _filter;

    /// <inheritdoc />
    public event Func<ServerLogEvent, Task>? LogReceived;

    /// <inheritdoc />
    public event Func<ServerLogDroppedEventSummary, Task>? DroppedEventsReceived;

    /// <inheritdoc />
    public event Func<ServerLogSource, Task>? SourceChanged;

    /// <inheritdoc />
    public event Func<ServerLogConnectionStatus, Task>? ConnectionStatusChanged;

    /// <inheritdoc />
    public async Task StartAsync(ServerLogFilter filter, CancellationToken cancellationToken = default)
    {
        _filter = ServerLogFilterMapper.ToLiveSubscription(filter);
        await PublishStatusAsync(ServerLogConnectionStatus.Connecting);

        _connection = await CreateConnectionAsync(cancellationToken);
        RegisterHandlers(_connection);

        try
        {
            await _connection.StartAsync(cancellationToken);
            await _connection.SendAsync("SubscribeAsync", _filter, cancellationToken);
            await PublishStatusAsync(ServerLogConnectionStatus.Connected);
        }
        catch (HttpRequestException e) when (e.StatusCode is HttpStatusCode.NotFound)
        {
            logger.LogWarning("The server logs hub was not found. Make sure the backend maps the server logs hub.");
            await PublishStatusAsync(ServerLogConnectionStatus.Unavailable);
        }
        catch (UnauthorizedAccessException)
        {
            await PublishStatusAsync(ServerLogConnectionStatus.Unauthorized);
        }
    }

    /// <inheritdoc />
    public async Task UpdateFilterAsync(ServerLogFilter filter, CancellationToken cancellationToken = default)
    {
        _filter = ServerLogFilterMapper.ToLiveSubscription(filter);

        if (_connection?.State != HubConnectionState.Connected)
            return;

        await _connection.SendAsync("UpdateFilterAsync", _filter, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReconnectAsync(ServerLogFilter filter, CancellationToken cancellationToken = default)
    {
        await DisposeConnectionAsync();
        await StartAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisposeConnectionAsync();
    }

    private async Task<HubConnection> CreateConnectionAsync(CancellationToken cancellationToken)
    {
        var hubUrl = new Uri(backendApiClientProvider.Url, "hubs/server-logs").ToString();
        HttpConnectionOptions? capturedOptions = null;

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options => capturedOptions = options)
            .WithAutomaticReconnect()
            .Build();

        if (capturedOptions != null)
            await httpConnectionOptionsConfigurator.ConfigureAsync(capturedOptions, cancellationToken);

        connection.Reconnecting += async _ => await PublishStatusAsync(ServerLogConnectionStatus.Reconnecting);
        connection.Reconnected += async _ =>
        {
            if (_filter != null)
                await connection.SendAsync("SubscribeAsync", _filter, cancellationToken);

            await PublishStatusAsync(ServerLogConnectionStatus.Connected);
        };
        connection.Closed += async _ => await PublishStatusAsync(ServerLogConnectionStatus.Disconnected);

        return connection;
    }

    private void RegisterHandlers(HubConnection connection)
    {
        connection.On<ServerLogEvent>("ReceiveLogEventAsync", async logEvent =>
        {
            if (LogReceived != null)
                await LogReceived(logEvent);
        });

        connection.On<ServerLogDroppedEventSummary>("ReceiveDroppedEventsAsync", async summary =>
        {
            if (DroppedEventsReceived != null)
                await DroppedEventsReceived(summary);
        });

        connection.On<ServerLogSource>("ReceiveSourceChangedAsync", async source =>
        {
            if (SourceChanged != null)
                await SourceChanged(source);
        });
    }

    private async Task DisposeConnectionAsync()
    {
        if (_connection == null)
            return;

        try
        {
            if (_connection.State == HubConnectionState.Connected)
                await _connection.SendAsync("UnsubscribeAsync");

            await _connection.StopAsync();
        }
        finally
        {
            await _connection.DisposeAsync();
            _connection = null;
            await PublishStatusAsync(ServerLogConnectionStatus.Disconnected);
        }
    }

    private async Task PublishStatusAsync(ServerLogConnectionStatus status)
    {
        if (ConnectionStatusChanged != null)
            await ConnectionStatusChanged(status);
    }
}
