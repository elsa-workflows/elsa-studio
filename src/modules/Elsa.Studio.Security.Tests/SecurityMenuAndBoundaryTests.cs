using Bunit;
using Elsa.Studio.Security.Components;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Menu;
using Elsa.Studio.Security.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class SecurityMenuTests
{
    [Fact]
    public async Task GetMenuItemsAsync_WhenAccessIsForbidden_ReturnsNoItems()
    {
        var menu = new SecurityMenu(new TestRoleAccessService(RoleAdministrationAccess.Forbidden));

        var items = await menu.GetMenuItemsAsync();

        Assert.Empty(items);
    }

    [Fact]
    public async Task GetMenuItemsAsync_WhenAccessIsReady_ReturnsRolesWithoutUsers()
    {
        var menu = new SecurityMenu(new TestRoleAccessService(new RoleAdministrationAccess(
            RoleAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: false, CanDelete: false)));

        var items = (await menu.GetMenuItemsAsync()).ToList();

        var item = Assert.Single(items);
        Assert.Equal("security/roles", item.Href);
        Assert.Equal("Security", item.Text);
        var roles = Assert.Single(item.SubMenuItems);
        Assert.Equal("Roles", roles.Text);
        Assert.Equal("security/roles", roles.Href);
        Assert.DoesNotContain(items.SelectMany(x => x.SubMenuItems), x => x.Text == "Users");
    }
}

public sealed class RoleAdministrationAccessBoundaryTests : BunitContext, IAsyncLifetime
{
    public RoleAdministrationAccessBoundaryTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void Render_WhenAccessIsForbidden_ShowsThePermissionRequiredState()
    {
        Services.AddSingleton<IRoleAdministrationAccessService>(new TestRoleAccessService(RoleAdministrationAccess.Forbidden));

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));

        cut.WaitForAssertion(() => Assert.Contains("Role administration access is required", cut.Markup));
        Assert.DoesNotContain("ready", cut.Markup);
    }

    [Fact]
    public void Render_WhenAccessIsUnavailable_ShowsTheUnavailableState()
    {
        Services.AddSingleton<IRoleAdministrationAccessService>(new TestRoleAccessService(RoleAdministrationAccess.Unavailable));

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("ready")));

        cut.WaitForAssertion(() => Assert.Contains("Role administration is unavailable", cut.Markup));
        Assert.DoesNotContain("ready", cut.Markup);
    }

    [Fact]
    public void Render_WhenAccessIsReady_RendersTheAuthorizedChildContent()
    {
        Services.AddSingleton<IRoleAdministrationAccessService>(new TestRoleAccessService(new RoleAdministrationAccess(
            RoleAdministrationAccessState.Ready, CanView: true, CanCreate: false, CanUpdate: false, CanDelete: false)));

        var cut = Render<RoleAdministrationAccessBoundary>(parameters =>
            parameters.Add(component => component.ChildContent, Child("authorized")));

        cut.WaitForAssertion(() => Assert.Contains("authorized", cut.Markup));
        Assert.DoesNotContain("Role administration access is required", cut.Markup);
        Assert.DoesNotContain("Role administration is unavailable", cut.Markup);
    }

    private static RenderFragment<RoleAdministrationAccess> Child(string text) =>
        access => builder => builder.AddContent(0, $"{text}:{access.CanView}");

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();
}

internal sealed class TestRoleAccessService(RoleAdministrationAccess access) : IRoleAdministrationAccessService
{
    public Task<RoleAdministrationAccess> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(access);
    public void Invalidate()
    {
    }
}
