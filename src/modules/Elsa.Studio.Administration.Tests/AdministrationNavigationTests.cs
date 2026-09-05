using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Labels.Menu;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Secrets.Menu;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Menu;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Elsa.Studio.Services;
using Microsoft.Extensions.Localization;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Administration.Tests;

public class AdministrationNavigationTests
{
    [Fact]
    public async Task DefaultMenuGroupProvider_UsesAdministrationAndRetainsSettingsAlias()
    {
        var groups = (await new DefaultMenuGroupProvider().GetMenuGroupsAsync()).ToList();

        var administration = Assert.Single(groups, x => x.Name == MenuItemGroups.Administration.Name);
        Assert.Equal("Administration", administration.Text);
        Assert.Equal("security", administration.Name);
        var legacySettings = typeof(MenuItemGroups).GetField("Settings");
        Assert.NotNull(legacySettings);
        Assert.Same(MenuItemGroups.Administration, legacySettings.GetValue(null));
    }

    [Fact]
    public async Task LabelsMenu_PlacesLabelsInAdministration()
    {
        var item = Assert.Single(await new LabelsMenu(new TestLocalizer()).GetMenuItemsAsync());

        Assert.Equal(MenuItemGroups.Administration.Name, item.GroupName);
        Assert.Equal(200, item.Order);
    }

    [Fact]
    public async Task SecretsMenu_PlacesSecretsInAdministration()
    {
        var item = Assert.Single(await new SecretsMenu(new EnabledRemoteFeatureProvider()).GetMenuItemsAsync());

        Assert.Equal(MenuItemGroups.Administration.Name, item.GroupName);
        Assert.Equal(300, item.Order);
    }

    [Fact]
    public async Task SecurityMenu_ExposesIdentityAndAccessInAdministration()
    {
        var item = Assert.Single(await new SecurityMenu([new StaticSecurityMenuContributor()]).GetMenuItemsAsync());

        Assert.Equal("Identity & access", item.Text);
        Assert.Equal(Icons.Material.Filled.ManageAccounts, item.Icon);
        Assert.Equal(MenuItemGroups.Administration.Name, item.GroupName);
        Assert.Equal(100, item.Order);
    }

    [Fact]
    public async Task IdentityMenu_UsesReadPermissionsAndCoreIdentityOrdering()
    {
        Assert.Equal("Elsa.Identity.ShellFeatures.Identity", Elsa.Studio.Security.Feature.RemoteFeatureName);

        var contributor = new IdentitySecurityMenuContributor(
            new EnabledRemoteFeatureProvider(),
            new TestIdentityPermissionService(IdentityClaimPermissions.ReadUser),
            new TestRoleAdministrationAccessService());

        var items = (await contributor.GetMenuItemsAsync()).ToList();

        Assert.Collection(items,
            user =>
            {
                Assert.Equal("Users", user.Text);
                Assert.Equal("security/users", user.Href);
                Assert.Equal(10, user.Order);
            },
            role =>
            {
                Assert.Equal("Roles", role.Text);
                Assert.Equal("security/roles", role.Href);
                Assert.Equal(20, role.Order);
            });
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    private sealed class EnabledRemoteFeatureProvider : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    private sealed class StaticSecurityMenuContributor : ISecurityMenuContributor
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) => new([
            new MenuItem { Href = "security/test", Text = "Test" }
        ]);
    }

    private sealed class TestIdentityPermissionService(params string[] permissions) : IIdentityPermissionService
    {
        private readonly IReadOnlySet<string> _permissions = permissions.ToHashSet(StringComparer.Ordinal);

        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) =>
            new(_permissions.Contains(permission));

        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) =>
            new(_permissions);
    }

    private sealed class TestRoleAdministrationAccessService : IRoleAdministrationAccessService
    {
        public Task<RoleAdministrationAccess> GetAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new RoleAdministrationAccess(
                RoleAdministrationAccessState.Ready,
                CanView: true,
                CanCreate: false,
                CanUpdate: false,
                CanDelete: false));

        public void Invalidate()
        {
        }
    }
}
