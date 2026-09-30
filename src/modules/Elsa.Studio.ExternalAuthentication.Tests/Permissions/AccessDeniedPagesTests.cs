using System.Security.Claims;
using Bunit;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;
using ConnectionsPage = Elsa.Studio.ExternalAuthentication.Pages.Connections.Index;
using IdentityLinksPage = Elsa.Studio.ExternalAuthentication.Pages.IdentityLinks.Index;
using SessionsPage = Elsa.Studio.ExternalAuthentication.Pages.Sessions.Index;

namespace Elsa.Studio.ExternalAuthentication.Tests.Permissions;

/// <summary>Every External Authentication page presents a missing view permission with the shared access-denied component and nothing else.</summary>
public sealed class AccessDeniedPagesTests : BunitContext
{
    public AccessDeniedPagesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IBackendApiClientProvider, UnreachableBackend>();
        Services.AddSingleton<IClipboard, NoClipboard>();
        Services.AddSingleton<AuthenticationStateProvider>(new UnprivilegedUser());
        Services.AddScoped<IExternalAuthenticationPermissionService, ExternalAuthenticationPermissionService>();
    }

    [Theory]
    [InlineData(typeof(ConnectionsPage), ExternalAuthenticationPermissions.Read)]
    [InlineData(typeof(IdentityLinksPage), ExternalAuthenticationPermissions.ManageLinks)]
    [InlineData(typeof(SessionsPage), ExternalAuthenticationPermissions.SessionsRead)]
    public void Page_WhenTheViewPermissionIsMissing_RendersOnlyTheSharedAccessDenied(Type page, string permission)
    {
        var cut = Render<DynamicComponent>(parameters => parameters.Add(component => component.Type, page));

        cut.WaitForAssertion(() => Assert.Contains(permission, cut.FindComponent<AccessDenied>().Markup));
        Assert.Empty(cut.FindAll("input, button, table"));
        Assert.DoesNotContain("You do not have permission", cut.Markup);
    }

    private sealed class UnprivilegedUser : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(new(new ClaimsIdentity("test"))));
    }

    private sealed class NoClipboard : IClipboard
    {
        public Task CopyText(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class UnreachableBackend : IBackendApiClientProvider
    {
        public Uri Url => throw new InvalidOperationException("A page without its view permission must not call the backend.");
        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class => throw new InvalidOperationException("A page without its view permission must not call the backend.");
    }
}
