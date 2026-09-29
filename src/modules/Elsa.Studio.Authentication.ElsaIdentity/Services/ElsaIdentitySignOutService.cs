using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;

namespace Elsa.Studio.Authentication.ElsaIdentity.Services;

/// <summary>
/// Signs out locally. Elsa Identity exposes no token revocation endpoint, so ending the session means discarding the
/// stored access and refresh tokens.
/// </summary>
public class ElsaIdentitySignOutService(
    IJwtAccessor jwtAccessor,
    AuthenticationStateProvider authenticationStateProvider,
    NavigationManager navigationManager) : ISignOutService
{
    /// <summary>
    /// The page the user lands on after signing out.
    /// </summary>
    public const string LoginPath = "/login";

    /// <inheritdoc />
    public async Task SignOutAsync()
    {
        await jwtAccessor.ClearTokensAsync();

        if (authenticationStateProvider is AccessTokenAuthenticationStateProvider accessTokenAuthenticationStateProvider)
            accessTokenAuthenticationStateProvider.NotifyAuthenticationStateChanged();

        // Force a reload so no in-memory state from the signed-out user survives.
        navigationManager.NavigateTo(LoginPath, forceLoad: true);
    }
}
