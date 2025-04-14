using Elsa.Studio.Login.Contracts;
using Microsoft.AspNetCore.Components;

namespace Elsa.Studio.Login.Services;

/// <inheritdoc/>
internal class ElsaIdentityAuthorizationService(NavigationManager NavigationManager) : IAuthorizationService
{
    /// <inheritdoc/>
    public Task RedirectToAuthorizationServer()
    {
        var returnUrl = NavigationManager.ToBaseRelativePath(NavigationManager.Uri);
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            NavigationManager.NavigateTo("login", true);
        }
        else
        {
            NavigationManager.NavigateTo($"login?returnUrl={returnUrl}", true);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ReceiveAuthorizationCode(string code, string? state, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }
}
