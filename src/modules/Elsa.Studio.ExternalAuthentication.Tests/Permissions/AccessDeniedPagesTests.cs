using System.Security.Claims;
using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Elsa.Studio.Testing;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;
using ConnectionEditPage = Elsa.Studio.ExternalAuthentication.Pages.Connections.Edit;
using ConnectionsPage = Elsa.Studio.ExternalAuthentication.Pages.Connections.Index;
using IdentityLinksPage = Elsa.Studio.ExternalAuthentication.Pages.IdentityLinks.Index;
using SessionsPage = Elsa.Studio.ExternalAuthentication.Pages.Sessions.Index;

namespace Elsa.Studio.ExternalAuthentication.Tests.Permissions;

/// <summary>Every External Authentication page presents a missing permission with the shared access-denied component and nothing else.</summary>
public sealed class AccessDeniedPagesTests : BunitContext
{
    private const string ViewConnections = "external-authentication/connections:view";

    private readonly StubPermissionService _shellPermissions = new();
    private readonly ClaimsIdentity _identity = new("test");

    public AccessDeniedPagesTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IBackendApiClientProvider, UnreachableBackend>();
        Services.AddSingleton<IClipboard, NoClipboard>();
        Services.AddSingleton<AuthenticationStateProvider>(new StaticAuthenticationStateProvider(new(_identity)));
        Services.AddSingleton<IPermissionService>(_shellPermissions);
        Services.AddScoped<IExternalAuthenticationPermissionService, ExternalAuthenticationPermissionService>();
        Services.AddSingleton<ICustomConnectionEditorRegistry, CustomConnectionEditorRegistry>();
    }

    [Theory]
    [InlineData(typeof(ConnectionsPage), ViewConnections)]
    [InlineData(typeof(ConnectionEditPage), ViewConnections)]
    [InlineData(typeof(IdentityLinksPage), ExternalAuthenticationPermissions.ManageLinks)]
    [InlineData(typeof(SessionsPage), ExternalAuthenticationPermissions.SessionsRead)]
    public void Page_DeclaresThePermissionItNeeds(Type page, string permission)
    {
        var expected = Permission.TryParse(permission, out var parsed) ? parsed : throw new FormatException(permission);

        Assert.Contains(expected, RequirePermissionAttribute.GetRequiredPermissions(page));
    }

    [Theory]
    [InlineData(typeof(ConnectionsPage))]
    [InlineData(typeof(ConnectionEditPage))]
    [InlineData(typeof(IdentityLinksPage))]
    [InlineData(typeof(SessionsPage))]
    public void ShellGuard_WhenThePermissionIsMissing_RendersOnlyTheSharedAccessDenied(Type page)
    {
        var declared = RequirePermissionAttribute.GetRequiredPermissions(page).Single();

        var cut = Render<PermissionPageGuard>(parameters => parameters
            .AddCascadingValue(new RouteData(page, new Dictionary<string, object?>()))
            .AddChildContent<DynamicComponent>(child => child.Add(x => x.Type, page)));

        Assert.Contains(declared.ToString(), cut.FindComponent<AccessDenied>().Markup);
        Assert.Empty(cut.FindAll("input, button, table"));
    }

    [Fact]
    public void NewConnection_WhenTheCreatePermissionIsMissing_RendersOnlyTheSharedAccessDenied()
    {
        _identity.AddClaim(new("permissions", ViewConnections));

        var cut = Render<ConnectionEditPage>();

        cut.WaitForAssertion(() => Assert.Contains("external-authentication/connections:create", cut.FindComponent<AccessDenied>().Markup));
        Assert.Empty(cut.FindAll("input, button, table"));
    }

    [Fact]
    public void NewConnection_WhenTheCreatePermissionIsHeld_DoesNotRenderAccessDenied()
    {
        _identity.AddClaim(new("permissions", ExternalAuthenticationPermissions.Create));

        var cut = Render<ConnectionEditPage>();

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("button")));
        Assert.Empty(cut.FindComponents<AccessDenied>());
    }

    private sealed class NoClipboard : IClipboard
    {
        public Task CopyText(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class UnreachableBackend : IBackendApiClientProvider
    {
        public Uri Url => throw new InvalidOperationException("A page without its permission must not call the backend.");
        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class => throw new InvalidOperationException("A page without its permission must not call the backend.");
    }
}
