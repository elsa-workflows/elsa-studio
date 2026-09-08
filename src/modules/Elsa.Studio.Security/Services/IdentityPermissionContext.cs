using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using Refit;

namespace Elsa.Studio.Security.Services;

/// <summary>
/// Loads the current caller's effective permissions once per Studio scope.
/// </summary>
public sealed class IdentityPermissionContext : IIdentityPermissionContext, IDisposable
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly IBackendApiClientProvider _apiClientProvider;
    private readonly ILogger<IdentityPermissionContext> _logger;
    private readonly AuthenticationStateProvider? _authenticationStateProvider;
    private readonly object _stateSync = new();
    private readonly CancellationTokenSource _lifetime = new();
    private IdentityPermissionSnapshot? _snapshot;
    private CancellationTokenSource? _activeLoadCancellation;
    private long _authenticationGeneration;
    private int _activeOperations;
    private bool _disposed;
    private bool _resourcesDisposed;

    public IdentityPermissionContext(
        IBackendApiClientProvider apiClientProvider,
        ILogger<IdentityPermissionContext> logger,
        AuthenticationStateProvider? authenticationStateProvider = null)
    {
        _apiClientProvider = apiClientProvider;
        _logger = logger;
        _authenticationStateProvider = authenticationStateProvider;
        if (_authenticationStateProvider is not null)
            _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public async Task<IdentityPermissionSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        lock (_stateSync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _activeOperations++;
        }

        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetime.Token);

        try
        {
            while (true)
            {
                var generation = Volatile.Read(ref _authenticationGeneration);
                lock (_stateSync)
                {
                    if (_snapshot != null && generation == _authenticationGeneration)
                        return _snapshot;
                }

                await _loadLock.WaitAsync(operationCancellation.Token);
                CancellationTokenSource? loadCancellation = null;

                try
                {
                    lock (_stateSync)
                    {
                        generation = _authenticationGeneration;
                        if (_snapshot != null)
                            return _snapshot;

                        loadCancellation = CancellationTokenSource.CreateLinkedTokenSource(operationCancellation.Token);
                        _activeLoadCancellation = loadCancellation;
                    }

                    IdentityPermissionSnapshot snapshot;
                    try
                    {
                        var api = await _apiClientProvider.GetApiAsync<IMePermissionsApi>(loadCancellation.Token);
                        var response = await api.GetAsync(loadCancellation.Token);
                        var grants = response.Grants
                            .GroupBy(x => x.Resource, StringComparer.Ordinal)
                            .ToDictionary(
                                group => group.Key,
                                group => (IReadOnlySet<string>)group
                                    .SelectMany(x => x.Verbs)
                                    .ToHashSet(StringComparer.Ordinal),
                                StringComparer.Ordinal);

                        snapshot = new IdentityPermissionSnapshot(IdentityPermissionSnapshotState.Ready, grants);
                    }
                    catch (ApiException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                    {
                        snapshot = IdentityPermissionSnapshot.Forbidden;
                    }
                    catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && generation != Volatile.Read(ref _authenticationGeneration))
                    {
                        continue;
                    }
                    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Loading the current Identity permissions timed out");
                        snapshot = IdentityPermissionSnapshot.Unavailable;
                    }
                    catch (Exception exception) when (exception is not OperationCanceledException)
                    {
                        _logger.LogWarning(exception, "Loading the current Identity permissions failed");
                        snapshot = IdentityPermissionSnapshot.Unavailable;
                    }

                    lock (_stateSync)
                    {
                        if (generation != _authenticationGeneration)
                            continue;

                        _snapshot = snapshot;
                        return snapshot;
                    }
                }
                finally
                {
                    lock (_stateSync)
                    {
                        if (ReferenceEquals(_activeLoadCancellation, loadCancellation))
                            _activeLoadCancellation = null;
                    }

                    loadCancellation?.Dispose();
                    _loadLock.Release();
                }
            }
        }
        finally
        {
            var disposeResources = false;
            lock (_stateSync)
            {
                _activeOperations--;
                if (_disposed && _activeOperations == 0 && !_resourcesDisposed)
                {
                    _resourcesDisposed = true;
                    disposeResources = true;
                }
            }

            if (disposeResources)
                DisposeResources();
        }
    }

    public void Invalidate()
    {
        lock (_stateSync)
        {
            if (_disposed)
                return;

            _authenticationGeneration++;
            _snapshot = null;
            _activeLoadCancellation?.Cancel();
        }
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> _) => Invalidate();

    public void Dispose()
    {
        if (_authenticationStateProvider is not null)
            _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;

        var disposeResources = false;
        lock (_stateSync)
        {
            if (_disposed)
                return;

            _disposed = true;
            _lifetime.Cancel();
            _activeLoadCancellation?.Cancel();

            if (_activeOperations == 0)
            {
                _resourcesDisposed = true;
                disposeResources = true;
            }
        }

        if (disposeResources)
            DisposeResources();
    }

    private void DisposeResources()
    {
        _loadLock.Dispose();
        _lifetime.Dispose();
    }
}
