using System.Security.Claims;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Xunit;

namespace Elsa.Studio.Core.Tests;

public class MenuPermissionTests
{
    [Theory]
    [InlineData("workflows/definitions:view")]
    [InlineData("workflows/definitions:*")]
    [InlineData("workflows/*:view")]
    [InlineData("workflows/*:*")]
    [InlineData("*")]
    public async Task CurrentUserPermissionService_MatchesExactAndWildcardGrants(string grant)
    {
        var service = new CurrentUserPermissionService(new TestAuthenticationStateProvider(grant));

        Assert.True(await service.HasAsync("workflows/definitions:view"));
    }

    [Theory]
    [InlineData("workflows/instances:view")]
    [InlineData("workflows:view")]
    [InlineData("workflows*:view")]
    [InlineData("")]
    public async Task CurrentUserPermissionService_RejectsUnrelatedOrMalformedGrants(string grant)
    {
        var service = new CurrentUserPermissionService(new TestAuthenticationStateProvider(grant));

        Assert.False(await service.HasAsync("workflows/definitions:view"));
    }

    [Fact]
    public async Task CurrentUserPermissionService_WhenAuthenticationIsNotRegistered_AllowsPermissions()
    {
        var service = new CurrentUserPermissionService();

        Assert.True(await service.HasAsync("workflows/definitions:view"));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task CurrentUserPermissionService_WhenAuthorizationIsDisabled_AllowsPermissionsWithoutEffectiveSource()
    {
        var service = new CurrentUserPermissionService(
            authenticationStateProvider: null,
            effectivePermissionSource: new StaticPermissionSource());

        Assert.True(await service.HasAsync("workflows/definitions:view"));
    }

    [Fact]
    public async Task CurrentUserPermissionService_FallsBackToEffectivePermissionsForAuthenticatedPrincipalWithoutClaims()
    {
        var source = new StaticPermissionSource("workflows/definitions:view");
        var service = new CurrentUserPermissionService(new TestAuthenticationStateProvider(), source);

        Assert.True(await service.HasAsync("workflows/definitions:view"));
        Assert.Equal(["workflows/definitions:view"], await service.ListAsync());
    }

    [Fact]
    public async Task CurrentUserPermissionService_FailsClosedWhenEffectivePermissionsAreUnavailable()
    {
        var service = new CurrentUserPermissionService(new TestAuthenticationStateProvider(), new StaticPermissionSource());

        Assert.False(await service.HasAsync("workflows/definitions:view"));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task CurrentUserPermissionService_ClaimsTakePrecedenceOverEffectivePermissionSource()
    {
        var source = new StaticPermissionSource("workflows/definitions:view");
        var service = new CurrentUserPermissionService(new TestAuthenticationStateProvider("dashboard:view"), source);

        Assert.False(await service.HasAsync("workflows/definitions:view"));
        Assert.Equal(["dashboard:view"], await service.ListAsync());
    }

    [Fact]
    public async Task CurrentUserPermissionService_DoesNotGrantPermissionsToUnauthenticatedPrincipal()
    {
        var service = new CurrentUserPermissionService(new UnauthenticatedStateProvider());

        Assert.False(await service.HasAsync("workflows/definitions:view"));
    }

    [Fact]
    public async Task CurrentUserPermissionService_DoesNotTrustPermissionClaimsFromUnauthenticatedPrincipal()
    {
        var service = new CurrentUserPermissionService(new UnauthenticatedStateProvider("workflows/definitions:view"));

        Assert.False(await service.HasAsync("workflows/definitions:view"));
    }

    [Fact]
    public async Task DefaultMenuService_FiltersProtectedChildrenAndKeepsAccessibleSiblings()
    {
        var protectedChild = new MenuItem
        {
            Text = "Definitions",
            Href = "workflows/definitions",
            RequiredPermission = "workflows/definitions:view"
        };
        var accessibleChild = new MenuItem { Text = "Public", Href = "public" };
        var parent = new MenuItem { Text = "Workflows", SubMenuItems = [protectedChild, accessibleChild] };
        var service = CreateMenuService(parent, "dashboard:view");

        var visibleParent = Assert.Single(await service.GetMenuItemsAsync());
        Assert.Equal("Public", Assert.Single(visibleParent.SubMenuItems).Text);
    }

    [Fact]
    public async Task DefaultMenuService_DoesNotMutateProviderTree_WhenPermissionsChange()
    {
        var protectedChild = new MenuItem
        {
            Text = "Definitions",
            Href = "workflows/definitions",
            RequiredPermission = "workflows/definitions:view",
            Order = 2f
        };
        var accessibleChild = new MenuItem
        {
            Text = "Public",
            Href = "public",
            Order = 1f
        };
        var parent = new MenuItem
        {
            Text = "Workflows",
            Href = "workflows",
            RequiredPermission = "workflows:view",
            Icon = "workflow-icon",
            Match = NavLinkMatch.All,
            Order = 3f,
            GroupName = "General",
            SubMenuItems = [protectedChild, accessibleChild]
        };
        var permissions = new MutablePermissionService("workflows:view");
        var service = CreateMenuService(parent, permissions);

        var firstParent = Assert.Single(await service.GetMenuItemsAsync());
        Assert.Equal(["Public"], firstParent.SubMenuItems.Select(x => x.Text));
        Assert.Same(protectedChild, parent.SubMenuItems.First());
        Assert.Equal(2, parent.SubMenuItems.Count);

        permissions.Grant("workflows/definitions:view");

        var secondParent = Assert.Single(await service.GetMenuItemsAsync());
        Assert.Equal(["Definitions", "Public"], secondParent.SubMenuItems.Select(x => x.Text));
        Assert.NotSame(parent, secondParent);
        Assert.NotSame(protectedChild, secondParent.SubMenuItems.First());
        Assert.Equal(parent.RequiredPermission, secondParent.RequiredPermission);
        Assert.Equal(parent.Icon, secondParent.Icon);
        Assert.Equal(parent.Href, secondParent.Href);
        Assert.Equal(parent.Match, secondParent.Match);
        Assert.Equal(parent.Order, secondParent.Order);
        Assert.Equal(parent.GroupName, secondParent.GroupName);
        Assert.Equal([2f, 1f], secondParent.SubMenuItems.Select(x => x.Order));
    }

    [Fact]
    public async Task DefaultMenuService_RemovesEmptyNavigationParents()
    {
        var parent = new MenuItem
        {
            Text = "Workflows",
            SubMenuItems =
            [
                new MenuItem
                {
                    Text = "Definitions",
                    Href = "workflows/definitions",
                    RequiredPermission = "workflows/definitions:view"
                }
            ]
        };
        var service = CreateMenuService(parent);

        Assert.Empty(await service.GetMenuItemsAsync());
    }

    private static DefaultMenuService CreateMenuService(MenuItem item, params string[] permissions) =>
        new([new StaticMenuProvider(item)], [], new StaticPermissionService(permissions));

    private static DefaultMenuService CreateMenuService(MenuItem item, ICurrentUserPermissionService permissionService) =>
        new([new StaticMenuProvider(item)], [], permissionService);

    private sealed class StaticMenuProvider(MenuItem item) : IMenuProvider
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IEnumerable<MenuItem>>([item]);
    }

    private sealed class StaticPermissionService(params string[] permissions) : ICurrentUserPermissionService
    {
        private readonly HashSet<string> _permissions = permissions.ToHashSet(StringComparer.Ordinal);

        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_permissions.Contains(permission) || _permissions.Contains("*"));

        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlySet<string>>(_permissions);
    }

    private sealed class MutablePermissionService(params string[] permissions) : ICurrentUserPermissionService
    {
        private readonly HashSet<string> _permissions = permissions.ToHashSet(StringComparer.Ordinal);

        public void Grant(string permission) => _permissions.Add(permission);

        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_permissions.Contains(permission) || _permissions.Contains("*"));

        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<IReadOnlySet<string>>(_permissions);
    }

    private sealed class StaticPermissionSource(params string[] permissions) : ICurrentUserPermissionSource
    {
        private readonly IReadOnlySet<string>? _permissions = permissions.Length == 0
            ? null
            : permissions.ToHashSet(StringComparer.Ordinal);

        public ValueTask<IReadOnlySet<string>?> GetAsync(CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(_permissions);
    }

    private sealed class TestAuthenticationStateProvider(params string[] permissions) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var claims = permissions.Select(x => new Claim("permissions", x));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
            return Task.FromResult(new AuthenticationState(principal));
        }
    }

    private sealed class UnauthenticatedStateProvider(params string[] permissions) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var claims = permissions.Select(x => new Claim("permissions", x));
            return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims))));
        }
    }
}
