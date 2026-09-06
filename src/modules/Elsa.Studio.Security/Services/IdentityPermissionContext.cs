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
    private IdentityPermissionSnapshot? _snapshot;

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
        if (_snapshot != null)
            return _snapshot;

        await _loadLock.WaitAsync(cancellationToken);
        try
        {
            if (_snapshot != null)
                return _snapshot;

            try
            {
                var api = await _apiClientProvider.GetApiAsync<IMePermissionsApi>(cancellationToken);
                var response = await api.GetAsync(cancellationToken);
                var grants = response.Grants
                    .GroupBy(x => x.Resource, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => (IReadOnlySet<string>)group
                            .SelectMany(x => x.Verbs)
                            .ToHashSet(StringComparer.Ordinal),
                        StringComparer.Ordinal);

                _snapshot = new IdentityPermissionSnapshot(IdentityPermissionSnapshotState.Ready, grants);
            }
            catch (ApiException exception) when (exception.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                _snapshot = IdentityPermissionSnapshot.Forbidden;
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Loading the current Identity permissions timed out");
                _snapshot = IdentityPermissionSnapshot.Unavailable;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                _logger.LogWarning(exception, "Loading the current Identity permissions failed");
                _snapshot = IdentityPermissionSnapshot.Unavailable;
            }

            return _snapshot;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Invalidate() => _snapshot = null;

    private void OnAuthenticationStateChanged(Task<AuthenticationState> _) => Invalidate();

    public void Dispose()
    {
        if (_authenticationStateProvider is not null)
            _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
        _loadLock.Dispose();
    }
}
