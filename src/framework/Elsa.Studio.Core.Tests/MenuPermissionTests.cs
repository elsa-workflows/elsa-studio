using System.Security.Claims;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.AspNetCore.Components.Authorization;
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

    private sealed class TestAuthenticationStateProvider(params string[] permissions) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var claims = permissions.Select(x => new Claim("permissions", x));
            var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
            return Task.FromResult(new AuthenticationState(principal));
        }
    }
}
