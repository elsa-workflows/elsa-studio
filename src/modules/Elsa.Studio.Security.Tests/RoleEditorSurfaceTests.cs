using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Components;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using MudBlazor;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class RoleEditorSurfaceTests : BunitContext, IAsyncLifetime
{
    public RoleEditorSurfaceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
    }

    [Fact]
    public void EditSurfaceShowsDirectCoveredUnverifiedAndRepairStates()
    {
        var roles = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles =
                [
                    new RoleSummary
                    {
                        Id = "auditors",
                        Name = "Auditors",
                        Permissions = ["workflows/definitions:update", "workflows/*:view", "WorkflowDefinitions:Publish"]
                    }
                ]
            }
        };
        var permissions = new StubPermissionsApi
        {
            Response = new PermissionCatalogResponse
            {
                Resources =
                [
                    new PermissionResourceDescriptor
                    {
                        Resource = "workflows/definitions",
                        DisplayName = "Definitions",
                        Description = "Workflow definitions.",
                        Category = "Workflows",
                        SupportedVerbs = ["view", "update"],
                        Verified = false
                    }
                ]
            }
        };
        Register(roles, permissions);

        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "auditors")
            .Add(x => x.Access, ReadyAccess));

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Edit role — Auditors", cut.Markup);
            Assert.Contains("Direct grant", cut.Markup);
            Assert.Contains("Covered by workflows/*:view", cut.Markup);
            Assert.Contains("Unverified · verified:false", cut.Markup);
            Assert.Contains("WorkflowDefinitions:Publish", cut.Markup);
            Assert.Contains("Save changes is disabled until issues are resolved", cut.Markup);
        });
    }

    [Fact]
    public void CreateSurfaceSendsNormalizedGrantsOnceAndNavigatesToCreatedRole()
    {
        var roles = new StubRolesApi
        {
            Created = new CreateRoleResponse { Id = "new-role", Name = "New role", Permissions = ["workflows/definitions:view"] }
        };
        var permissions = new StubPermissionsApi
        {
            Response = new PermissionCatalogResponse
            {
                Resources =
                [
                    new PermissionResourceDescriptor
                    {
                        Resource = "workflows/definitions",
                        DisplayName = "Definitions",
                        Category = "Workflows",
                        SupportedVerbs = ["view"]
                    }
                ]
            }
        };
        Register(roles, permissions);

        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("New role", cut.Markup));

        cut.Find("input[aria-label='Role name']").Input("  New role  ");
        cut.Find("input[aria-label='workflows/definitions:view']").Change(true);
        cut.FindAll("button").Single(x => x.TextContent.Contains("Create role", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, roles.CreateCalls));
        Assert.Equal("New role", roles.LastCreate!.Name);
        Assert.Equal(["workflows/definitions:view"], roles.LastCreate.Permissions);
        Assert.EndsWith("/security/roles/new-role", Services.GetRequiredService<NavigationManager>().Uri, StringComparison.Ordinal);
    }

    [Fact]
    public void EditDeleteAction_OpensTheSameSharedDeletionDialog()
    {
        var roles = new StubRolesApi
        {
            Response = new ListRolesResponse
            {
                Roles = [new RoleSummary { Id = "auditors", Name = "Auditors" }]
            }
        };
        Register(roles, new StubPermissionsApi());
        var provider = Render<MudDialogProvider>();
        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "auditors")
            .Add(x => x.Access, ReadyAccess with { CanDelete = true }));
        cut.WaitForAssertion(() => Assert.Contains("Edit role — Auditors", cut.Markup));

        cut.FindAll("button").Single(x => x.TextContent.Contains("Delete role", StringComparison.Ordinal)).Click();

        provider.WaitForAssertion(() =>
            Assert.Equal("auditors", provider.FindComponent<DeleteRoleDialog>().Instance.RoleId));
    }

    private void Register(IRolesApi roles, IPermissionsApi permissions)
    {
        Services.AddSingleton<IBackendApiClientProvider>(new StubBackendApiClientProvider(roles, permissions));
        Services.AddSingleton<IRoleDeletionService>(new StubRoleDeletionService());
    }

    private static RoleAdministrationAccess ReadyAccess =>
        new(RoleAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: true, CanDelete: false);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await base.DisposeAsync();

    private sealed class StubBackendApiClientProvider(IRolesApi roles, IPermissionsApi permissions) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://localhost/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class =>
            ValueTask.FromResult(typeof(T) == typeof(IRolesApi) ? (T)(object)roles : (T)(object)permissions);
    }

    private sealed class StubRolesApi : IRolesApi
    {
        public ListRolesResponse Response { get; set; } = new();
        public CreateRoleResponse Created { get; set; } = new();
        public int CreateCalls { get; private set; }
        public CreateRoleRequest? LastCreate { get; private set; }

        public Task<ListRolesResponse> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(Response);

        public Task<CreateRoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            LastCreate = request;
            return Task.FromResult(Created);
        }

        public Task<UpdateRoleResponse> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<RoleDeletionImpactResponse> GetDeletionImpactAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemediateAndDeleteAsync(string id, RoleRemediationRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class StubPermissionsApi : IPermissionsApi
    {
        public PermissionCatalogResponse Response { get; set; } = new();

        public Task<PermissionCatalogResponse> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(Response);

        public Task<PermissionReachResponse> GetReachAsync(string resource, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PermissionReachResponse { Resource = resource, Covers = ["workflows/definitions"], Count = 1 });
    }

}
