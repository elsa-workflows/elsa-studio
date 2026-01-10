using Elsa.Studio.Authentication.ElsaAuth.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Authentication.ElsaAuth.Services;

/// <inheritdoc />
public class ElsaIdentityEndSessionService(NavigationManager navigationManager) : IEndSessionService
{
    /// <inheritdoc />
    public Task EndSessionAsync(CancellationToken cancellationToken = default)
    {
        navigationManager.NavigateTo("/logout", forceLoad: true);
        return Task.CompletedTask;
    }
}

