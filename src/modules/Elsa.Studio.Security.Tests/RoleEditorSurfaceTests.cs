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
                        SupportedVerbs = ["view", "update", "publish"],
                        NonCoreVerbs = ["publish"],
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
            Assert.Equal("Edit role — Auditors", cut.Find("h1").TextContent.Trim());
            Assert.Contains("role-editor-title", cut.Find("h1").ClassList);
            Assert.Equal("Roles", cut.Find(".role-editor-back").TextContent.Trim());
            var summary = cut.Find(".role-editor-summary");
            var actions = cut.Find(".role-editor-actions");
            Assert.Null(summary.QuerySelector(".role-editor-actions"));
            var actionButtons = actions.QuerySelectorAll("button");
            Assert.Equal(["Cancel", "Save changes"], actionButtons.Select(button => button.TextContent.Trim()));
            Assert.All(actionButtons, button => Assert.Contains("mud-button-text", button.ClassList));
            Assert.Contains("mud-button-text-primary", actionButtons[1].ClassList);
            Assert.Contains("role-editor-primary-action", actionButtons[1].ClassList);
            Assert.DoesNotContain("mud-breadcrumbs", cut.Markup);
            Assert.DoesNotContain("Direct grant", cut.Markup);
            var details = cut.Find(".role-editor-details");
            Assert.Equal("The role ID is read-only.", details.Children[1].TextContent.Trim());
            Assert.Contains("role-editor-id-note", details.Children[1].ClassList);
            Assert.True(cut.Find("input[aria-label='workflows/definitions:update']").HasAttribute("checked"));
            var coveredPermission = cut.Find("input[aria-label='workflows/definitions:view']");
            Assert.True(coveredPermission.HasAttribute("checked"));
            Assert.True(coveredPermission.HasAttribute("disabled"));
            Assert.Contains("Covered by workflows/*:view", cut.Markup);
            Assert.Contains("role-covered-grant", cut.Markup);
            Assert.Contains("Unverified · verified:false", cut.Markup);
            Assert.DoesNotContain("Non-core:", cut.Markup);
            Assert.NotNull(cut.Find("input[aria-label='workflows/definitions:publish']"));
            Assert.Contains("WorkflowDefinitions:Publish", cut.Markup);
            Assert.Contains("Save changes is disabled until issues are resolved", cut.Markup);
        });
    }

    [Fact]
    public void ExplicitReplacementPersistsAnExactGrantEvenWhenCoveredByWildcard()
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
                        Permissions = ["workflows/*:view", "legacy:grant"]
                    }
                ]
            },
            Updated = new UpdateRoleResponse
            {
                Id = "auditors",
                Name = "Auditors",
                Permissions = ["workflows/*:view", "workflows/definitions:view"]
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
                        Category = "Workflows",
                        SupportedVerbs = ["view"]
                    }
                ]
            }
        };
        Register(roles, permissions);

        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "auditors")
            .Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("legacy:grant", cut.Markup));

        cut.Find("input[placeholder='resource:verb or wildcard']").Change("workflows/definitions:view");
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Replace").Click();
        cut.FindAll("button").Single(x => x.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, roles.UpdateCalls));
        Assert.Contains("workflows/definitions:view", roles.LastUpdate!.Permissions!);
    }

    [Fact]
    public async Task SaveIsSingleFlightWhileTheFirstRequestIsInProgress()
    {
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var roles = new StubRolesApi
        {
            Created = new CreateRoleResponse { Id = "new-role", Name = "New role" }
        };
        roles.CreateHandler = async cancellationToken =>
        {
            started.SetResult();
            await release.Task.WaitAsync(cancellationToken);
            return roles.Created;
        };
        Register(roles, new StubPermissionsApi());

        var cut = Render<RoleEditorSurface>(parameters => parameters.Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("New role", cut.Markup));
        Assert.Equal("New role", cut.Find("h1").TextContent.Trim());
        Assert.Equal("Roles", cut.Find(".role-editor-back").TextContent.Trim());
        Assert.Empty(cut.FindAll(".role-editor-id"));
        cut.Find("input[aria-label='Role name']").Input("New role");
        var save = cut.FindAll("button").Single(x => x.TextContent.Contains("Create role", StringComparison.Ordinal));

        var first = save.ClickAsync();
        await Task.Yield();
        var currentSave = cut.FindAll("button").Single(x => x.TextContent.Contains("Saving", StringComparison.Ordinal));
        var second = currentSave.ClickAsync();
        await started.Task;
        release.SetResult();
        await Task.WhenAll(first, second);

        Assert.Equal(1, roles.CreateCalls);
    }

    [Fact]
    public void ReachProviderFailureIsShownAsRecoverableGrantError()
    {
        var roles = new StubRolesApi();
        var permissions = new StubPermissionsApi();
        var provider = new StubBackendApiClientProvider(roles, permissions)
        {
            GetApiException = (type, count) => type == typeof(IPermissionsApi) && count > 1
                ? new InvalidOperationException("permissions provider unavailable")
                : null
        };
        Services.AddSingleton<IBackendApiClientProvider>(provider);
        Services.AddSingleton<IRoleDeletionService>(new StubRoleDeletionService());
        Render<MudPopoverProvider>();

        var cut = Render<RoleEditorSurface>(parameters => parameters.Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("New role", cut.Markup));
        cut.FindAll("[role='tab']").Single(x => x.TextContent.Contains("Advanced grants", StringComparison.OrdinalIgnoreCase)).Click();
        cut.Find("input[placeholder='workflows/*:view or *']").Change("workflows/*:view");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Add advanced grant", StringComparison.OrdinalIgnoreCase)).Click();

        Assert.Equal(2, provider.PermissionsApiCalls);
        cut.WaitForAssertion(() => Assert.Contains("Role administration is unavailable right now", cut.Markup));
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
    public void GlobalBulkActionSelectsAndClearsEveryExactPermission()
    {
        Register(new StubRolesApi(), new StubPermissionsApi { Response = CreateBulkPermissionCatalog() });
        var cut = Render<RoleEditorSurface>(parameters => parameters.Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("button[aria-label='Select all exact permissions']")));

        cut.Find("input[aria-label='ai/chat:execute']").Change(true);
        cut.Find("button[aria-label='Select all exact permissions']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.All(cut.FindAll(".role-verb-option input"), checkbox => Assert.True(checkbox.HasAttribute("checked")));
            Assert.NotNull(cut.Find("button[aria-label='Clear all exact permissions']"));
        });

        cut.Find("button[aria-label='Clear all exact permissions']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.All(cut.FindAll(".role-verb-option input"), checkbox => Assert.False(checkbox.HasAttribute("checked")));
            Assert.NotNull(cut.Find("button[aria-label='Select all exact permissions']"));
        });
    }

    [Fact]
    public void CategoryBulkActionOnlyTogglesPermissionsInThatCategory()
    {
        Register(new StubRolesApi(), new StubPermissionsApi { Response = CreateBulkPermissionCatalog() });
        var cut = Render<RoleEditorSurface>(parameters => parameters.Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("button[aria-label='Select all permissions in Workflows']")));

        var selectButton = cut.Find("button[aria-label='Select all permissions in Workflows']");
        Assert.DoesNotContain("mud-panel-expanded", selectButton.Closest(".mud-expand-panel")!.ClassList);
        selectButton.Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Find("input[aria-label='workflows/definitions:view']").HasAttribute("checked"));
            Assert.True(cut.Find("input[aria-label='workflows/definitions:write']").HasAttribute("checked"));
            Assert.False(cut.Find("input[aria-label='ai/chat:execute']").HasAttribute("checked"));
            var clearButton = cut.Find("button[aria-label='Clear all permissions in Workflows']");
            Assert.DoesNotContain("mud-panel-expanded", clearButton.Closest(".mud-expand-panel")!.ClassList);
        });

        cut.Find("button[aria-label='Clear all permissions in Workflows']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.False(cut.Find("input[aria-label='workflows/definitions:view']").HasAttribute("checked"));
            Assert.False(cut.Find("input[aria-label='workflows/definitions:write']").HasAttribute("checked"));
        });
    }

    [Fact]
    public void ClearingCategoryPermissionsDoesNotCollapseAnExpandedCategory()
    {
        var roles = CreateRoleFixture(
            ["workflows/definitions:view", "workflows/definitions:write"],
            []);
        Register(roles, new StubPermissionsApi { Response = CreateBulkPermissionCatalog() });
        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "operators")
            .Add(x => x.Access, ReadyAccess));

        cut.WaitForAssertion(() =>
        {
            var clearButton = cut.Find("button[aria-label='Clear all permissions in Workflows']");
            Assert.Contains("mud-panel-expanded", clearButton.Closest(".mud-expand-panel")!.ClassList);
        });

        cut.Find("button[aria-label='Clear all permissions in Workflows']").Click();

        cut.WaitForAssertion(() =>
        {
            var selectButton = cut.Find("button[aria-label='Select all permissions in Workflows']");
            Assert.Contains("mud-panel-expanded", selectButton.Closest(".mud-expand-panel")!.ClassList);
        });
    }

    [Fact]
    public void BulkActionOnlyChangesPermissionsMatchingTheCurrentFilter()
    {
        Register(new StubRolesApi(), new StubPermissionsApi { Response = CreateBulkPermissionCatalog() });
        var cut = Render<RoleEditorSurface>(parameters => parameters.Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("button[aria-label='Select all exact permissions']")));

        cut.Find("input[placeholder='Search by name, ID, or permission']").Input("AI chat");
        cut.Find("button[aria-label='Select all matching exact permissions']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Find("input[aria-label='ai/chat:execute']").HasAttribute("checked"));
            Assert.Empty(cut.FindAll("input[aria-label='workflows/definitions:view']"));
            Assert.NotNull(cut.Find("button[aria-label='Clear all matching exact permissions']"));
        });

        cut.Find("input[placeholder='Search by name, ID, or permission']").Input(string.Empty);
        cut.WaitForAssertion(() =>
        {
            Assert.True(cut.Find("input[aria-label='ai/chat:execute']").HasAttribute("checked"));
            Assert.False(cut.Find("input[aria-label='workflows/definitions:view']").HasAttribute("checked"));
            Assert.False(cut.Find("input[aria-label='workflows/definitions:write']").HasAttribute("checked"));
        });
    }

    [Fact]
    public void BulkSelectionDoesNotMaterializePermissionsCoveredByAnAdvancedGrant()
    {
        var roles = CreateRoleFixture(
            ["workflows/*:view"],
            ["workflows/*:view", "workflows/definitions:write"]);
        Register(roles, CreateWorkflowPermissionCatalogApi());
        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "operators")
            .Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Covered by workflows/*:view", cut.Markup);
            var coveredPermission = cut.Find("input[aria-label='workflows/definitions:view']");
            Assert.True(coveredPermission.HasAttribute("checked"));
            Assert.True(coveredPermission.HasAttribute("disabled"));
        });

        cut.Find("button[aria-label='Select all exact permissions']").Click();
        cut.FindAll("button").Single(x => x.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, roles.UpdateCalls));
        Assert.Equal(["workflows/*:view", "workflows/definitions:write"], roles.LastUpdate!.Permissions);
    }

    [Fact]
    public void BulkClearRemovesPreExistingExactPermissionsCoveredByAnAdvancedGrant()
    {
        var roles = CreateRoleFixture(
            ["workflows/*:view", "workflows/definitions:view", "workflows/definitions:write"],
            ["workflows/*:view", "workflows/definitions:view"]);
        Register(roles, CreateWorkflowPermissionCatalogApi());
        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "operators")
            .Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("button[aria-label='Clear all exact permissions']"));
            var directCoveredPermission = cut.Find("input[aria-label='workflows/definitions:view']");
            Assert.True(directCoveredPermission.HasAttribute("checked"));
            Assert.False(directCoveredPermission.HasAttribute("disabled"));
        });

        cut.Find("button[aria-label='Clear all exact permissions']").Click();
        cut.FindAll("button").Single(x => x.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, roles.UpdateCalls));
        Assert.Equal(["workflows/*:view"], roles.LastUpdate!.Permissions);
    }

    [Fact]
    public void BulkClearRemovesDirectCoveredPermissionsWhenAllCatalogPermissionsAreCovered()
    {
        var roles = CreateRoleFixture(
            ["workflows/*:*", "workflows/definitions:view"],
            ["workflows/*:*"]);
        Register(roles, CreateWorkflowPermissionCatalogApi());
        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "operators")
            .Add(x => x.Access, ReadyAccess));

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("button[aria-label='Clear all exact permissions']"));
            var directCoveredPermission = cut.Find("input[aria-label='workflows/definitions:view']");
            Assert.True(directCoveredPermission.HasAttribute("checked"));
            Assert.False(directCoveredPermission.HasAttribute("disabled"));
        });

        cut.Find("button[aria-label='Clear all exact permissions']").Click();
        cut.FindAll("button").Single(x => x.TextContent.Contains("Save changes", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal(1, roles.UpdateCalls));
        Assert.Equal(["workflows/*:*"], roles.LastUpdate!.Permissions);
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

        var delete = cut.FindAll("button").Single(x => x.TextContent.Contains("Delete role", StringComparison.Ordinal));
        Assert.Contains("mud-button-text", delete.ClassList);
        Assert.Contains("mud-button-text-error", delete.ClassList);
        delete.Click();

        provider.WaitForAssertion(() =>
            Assert.Equal("auditors", provider.FindComponent<DeleteRoleDialog>().Instance.RoleId));
    }

    [Fact]
    public void SuccessfulAdvancedGrantClearsAnEarlierValidationError()
    {
        Register(new StubRolesApi(), new StubPermissionsApi());
        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("New role", cut.Markup));

        cut.FindAll("[role='tab']").Single(x => x.TextContent.Contains("Advanced grants", StringComparison.OrdinalIgnoreCase)).Click();
        cut.Find("input[placeholder='workflows/*:view or *']").Change("not-a-grant");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Add advanced grant", StringComparison.OrdinalIgnoreCase)).Click();
        Assert.Contains("Enter a valid grant", cut.Markup);

        cut.Find("input[placeholder='workflows/*:view or *']").Change("workflows/*:view");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Add advanced grant", StringComparison.OrdinalIgnoreCase)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("Enter a valid grant", cut.Markup);
            Assert.Contains("workflows/*:view", cut.Markup);
        });
    }

    [Fact]
    public void AdvancedGrantCanBeEditedInPlaceAndSubmitted()
    {
        var roles = new StubRolesApi
        {
            Created = new CreateRoleResponse { Id = "workflow-admin", Name = "Workflow admin" }
        };
        Register(roles, new StubPermissionsApi());
        var cut = Render<RoleEditorSurface>(parameters => parameters.Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("New role", cut.Markup));

        cut.Find("input[aria-label='Role name']").Input("Workflow admin");
        cut.FindAll("[role='tab']").Single(x => x.TextContent.Contains("Advanced grants", StringComparison.OrdinalIgnoreCase)).Click();
        cut.Find("input[placeholder='workflows/*:view or *']").Change("workflows/*:view");
        cut.FindAll("button").Single(x => x.TextContent.Contains("Add advanced grant", StringComparison.OrdinalIgnoreCase)).Click();
        Assert.DoesNotContain("Broad access", cut.Markup);
        cut.Find("button[aria-label='Edit advanced grant workflows/*:view']").Click();
        Assert.True(cut.FindAll("button").Single(x => x.TextContent.Contains("Create role", StringComparison.Ordinal)).HasAttribute("disabled"));
        cut.Find("input[aria-label='Grant expression for workflows/*:view']").Input(" workflows/*:* ");
        cut.Find("button[aria-label='Save advanced grant workflows/*:view']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("workflows/*:*", cut.Markup);
            Assert.Empty(cut.FindAll("button[aria-label='Edit advanced grant workflows/*:view']"));
            Assert.NotNull(cut.Find("button[aria-label='Edit advanced grant workflows/*:*']"));
            Assert.False(cut.FindAll("button").Single(x => x.TextContent.Contains("Create role", StringComparison.Ordinal)).HasAttribute("disabled"));
        });

        cut.FindAll("button").Single(x => x.TextContent.Contains("Create role", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, roles.CreateCalls));
        Assert.Equal(["workflows/*:*"], roles.LastCreate!.Permissions);
    }

    [Fact]
    public void AdvancedGrantCoverageListsPermissionIdentifiersWithoutRepeatingStorageState()
    {
        var cut = Render<RoleAdvancedGrantCard>(parameters => parameters
            .Add(x => x.Grant, "*")
            .Add(x => x.Reach, new PermissionReachResponse
            {
                Covers = ["ai/capabilities", "ai/chat"],
                Count = 2
            }));

        cut.Find("button[aria-label='Show current coverage for *']").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("ai/capabilities:*", cut.Markup);
            Assert.Contains("ai/chat:*", cut.Markup);
            Assert.Contains("Reach is calculated for this wildcard only", cut.Markup);
            Assert.DoesNotContain("Covered, not stored", cut.Markup);
        });
    }

    [Fact]
    public void AdvancedGrantEditRejectsInvalidAndDuplicateValuesAndCanBeCancelled()
    {
        Register(new StubRolesApi(), new StubPermissionsApi());
        var cut = Render<RoleEditorSurface>(parameters => parameters.Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.Contains("New role", cut.Markup));
        cut.FindAll("[role='tab']").Single(x => x.TextContent.Contains("Advanced grants", StringComparison.OrdinalIgnoreCase)).Click();

        AddAdvancedGrant(cut, "identity/roles:*");
        AddAdvancedGrant(cut, "workflows/*:view");
        cut.Find("button[aria-label='Edit advanced grant workflows/*:view']").Click();
        var editor = cut.Find("input[aria-label='Grant expression for workflows/*:view']");

        editor.Input("workflows/definitions:view");
        cut.Find("button[aria-label='Save advanced grant workflows/*:view']").Click();
        cut.WaitForAssertion(() => Assert.Contains("This is an exact permission", cut.Markup));

        editor = cut.Find("input[aria-label='Grant expression for workflows/*:view']");
        editor.Input("identity/roles:*");
        cut.Find("button[aria-label='Save advanced grant workflows/*:view']").Click();
        cut.WaitForAssertion(() => Assert.Contains("This advanced grant already exists", cut.Markup));

        cut.Find("button[aria-label='Cancel editing advanced grant workflows/*:view']").Click();
        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll("input[aria-label='Grant expression for workflows/*:view']"));
            Assert.NotNull(cut.Find("button[aria-label='Edit advanced grant workflows/*:view']"));
            Assert.NotNull(cut.Find("button[aria-label='Edit advanced grant identity/roles:*']"));
        });
    }

    [Fact]
    public void ExactPermissionSearch_MatchesTheFullPermissionIdentifier()
    {
        Register(new StubRolesApi(), new StubPermissionsApi
        {
            Response = new PermissionCatalogResponse
            {
                Resources =
                [
                    new PermissionResourceDescriptor
                    {
                        Resource = "workflows/definitions/labels",
                        DisplayName = "Definition labels",
                        Category = "Workflows",
                        SupportedVerbs = ["view"]
                    }
                ]
            }
        });

        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.Access, ReadyAccess));
        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[aria-label='workflows/definitions/labels:view']")));

        cut.Find("input[placeholder='Search by name, ID, or permission']").Input("workflows/definitions/labels:view");

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("input[aria-label='workflows/definitions/labels:view']")));
    }

    [Fact]
    public void ResponsiveLayout_ExposesStableHooksForPermissionTabsAndLegacyRepair()
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
                        Permissions = ["legacy:grant"]
                    }
                ]
            }
        };
        Register(roles, new StubPermissionsApi());

        var cut = Render<RoleEditorSurface>(parameters => parameters
            .Add(x => x.RoleId, "auditors")
            .Add(x => x.Access, ReadyAccess));

        cut.WaitForAssertion(() => Assert.Contains("Edit role — Auditors", cut.Markup));

        var tabs = cut.Find(".role-permissions-tabs");
        Assert.Equal(2, tabs.QuerySelectorAll("[role='tab']").Length);
        Assert.Contains("Exact permissions", tabs.TextContent, StringComparison.Ordinal);
        Assert.Contains("Advanced grants", tabs.TextContent, StringComparison.Ordinal);

        var repairActions = cut.Find(".role-unresolved-repair-actions");
        Assert.NotNull(repairActions.QuerySelector("input[placeholder='resource:verb or wildcard']"));
        Assert.Equal(
            ["Replace", "Remove"],
            repairActions.QuerySelectorAll("button").Select(x => x.TextContent.Trim()).ToArray());
    }

    private void Register(IRolesApi roles, IPermissionsApi permissions)
    {
        Services.AddSingleton<IBackendApiClientProvider>(new StubBackendApiClientProvider(roles, permissions));
        Services.AddSingleton<IRoleDeletionService>(new StubRoleDeletionService());
        Render<MudPopoverProvider>();
    }

    private static PermissionCatalogResponse CreateBulkPermissionCatalog() =>
        new()
        {
            Resources =
            [
                new PermissionResourceDescriptor
                {
                    Resource = "ai/chat",
                    DisplayName = "AI chat",
                    Category = "AI",
                    SupportedVerbs = ["execute"]
                },
                new PermissionResourceDescriptor
                {
                    Resource = "workflows/definitions",
                    DisplayName = "Workflow definitions",
                    Category = "Workflows",
                    SupportedVerbs = ["view", "write"]
                }
            ]
        };

    private static StubRolesApi CreateRoleFixture(string[] permissions, string[] updatedPermissions) =>
        new()
        {
            Response = new ListRolesResponse
            {
                Roles = [new RoleSummary { Id = "operators", Name = "Operators", Permissions = permissions }]
            },
            Updated = new UpdateRoleResponse
            {
                Id = "operators",
                Name = "Operators",
                Permissions = updatedPermissions
            }
        };

    private static StubPermissionsApi CreateWorkflowPermissionCatalogApi() =>
        new()
        {
            Response = new PermissionCatalogResponse
            {
                Resources =
                [
                    new PermissionResourceDescriptor
                    {
                        Resource = "workflows/definitions",
                        DisplayName = "Workflow definitions",
                        Category = "Workflows",
                        SupportedVerbs = ["view", "write"]
                    }
                ]
            }
        };

    private static void AddAdvancedGrant(IRenderedComponent<RoleEditorSurface> cut, string grant)
    {
        cut.Find("input[placeholder='workflows/*:view or *']").Change(grant);
        cut.FindAll("button").Single(x => x.TextContent.Contains("Add advanced grant", StringComparison.OrdinalIgnoreCase)).Click();
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find($"button[aria-label='Edit advanced grant {grant}']")));
    }

    private static RoleAdministrationAccess ReadyAccess =>
        new(RoleAdministrationAccessState.Ready, CanView: true, CanCreate: true, CanUpdate: true, CanDelete: false);

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync() => await base.DisposeAsync();

    private sealed class StubBackendApiClientProvider(IRolesApi roles, IPermissionsApi permissions) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://localhost/");
        public Func<Type, int, Exception?>? GetApiException { get; init; }
        public int PermissionsApiCalls => _permissionsApiCalls;
        private int _permissionsApiCalls;

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            var count = typeof(T) == typeof(IPermissionsApi) ? ++_permissionsApiCalls : 0;
            var exception = GetApiException?.Invoke(typeof(T), count);
            if (exception is not null)
                return ValueTask.FromException<T>(exception);

            return ValueTask.FromResult(typeof(T) == typeof(IRolesApi) ? (T)(object)roles : (T)(object)permissions);
        }
    }

    private sealed class StubRolesApi : IRolesApi
    {
        public ListRolesResponse Response { get; set; } = new();
        public CreateRoleResponse Created { get; set; } = new();
        public int CreateCalls { get; private set; }
        public CreateRoleRequest? LastCreate { get; private set; }
        public int UpdateCalls { get; private set; }
        public UpdateRoleRequest? LastUpdate { get; private set; }
        public Func<CancellationToken, Task<CreateRoleResponse>>? CreateHandler { get; set; }
        public UpdateRoleResponse Updated { get; set; } = new();

        public Task<ListRolesResponse> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(Response);

        public Task<CreateRoleResponse> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            LastCreate = request;
            return CreateHandler?.Invoke(cancellationToken) ?? Task.FromResult(Created);
        }

        public Task<UpdateRoleResponse> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            LastUpdate = request;
            return Task.FromResult(Updated);
        }
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
