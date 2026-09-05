using System.Diagnostics.CodeAnalysis;
using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using RoleEditor = Elsa.Studio.Security.Pages.Role;
using RolesPage = Elsa.Studio.Security.Pages.Roles;
using UserEditor = Elsa.Studio.Security.Pages.User;
using UsersPage = Elsa.Studio.Security.Pages.Users;

namespace Elsa.Studio.Administration.Tests;

public sealed class IdentityManagementTests : BunitContext, IAsyncLifetime
{
    private readonly UsersApi _users = new();
    private readonly RolesApi _roles = new();
    private readonly PermissionService _permissionService = new("*");
    private readonly Clipboard _clipboard = new();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public IdentityManagementTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IBackendApiClientProvider>(new ApiProvider(_users, _roles));
        Services.AddSingleton<IIdentityPermissionService>(_permissionService);
        Services.AddSingleton<IClipboard>(_clipboard);
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void UserList_RendersRolesAndFiltersLocally()
    {
        _users.Users =
        [
            new() { Id = "user-1", Name = "alice", Roles = ["admin"], TenantId = null },
            new() { Id = "user-2", Name = "bob", Roles = ["operator"], TenantId = "tenant-a" }
        ];

        var cut = Render<UsersPage>();
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("alice", cut.Markup);
            Assert.Contains("operator", cut.Markup);
            Assert.Contains("tenant-a", cut.Markup);
            Assert.Contains("Open user alice", cut.Markup);
            Assert.Equal(Breakpoint.Md, cut.FindComponent<MudTable<UserSummary>>().Instance.Breakpoint);
        });

        cut.Find("input[placeholder='Search users']").Input("operator");

        cut.WaitForAssertion(() =>
        {
            Assert.DoesNotContain("alice", cut.Markup);
            Assert.Contains("bob", cut.Markup);
        });
    }

    [Fact]
    public async Task UserList_PresentsUserIdSeparatelyAndCopiesTheFullValue()
    {
        _users.Users = [new() { Id = "user-with-a-long-id", Name = "alice", Roles = ["admin"] }];

        var cut = Render<UsersPage>();
        cut.WaitForAssertion(() =>
        {
            var row = cut.Find("tbody tr");
            Assert.Contains("User ID", cut.Markup);
            Assert.Equal("alice", row.QuerySelector("td[data-label='Name']")?.TextContent.Trim());
            Assert.Equal("user-with-a-long-id", row.QuerySelector("td[data-label='User ID'] .identity-id-value")?.TextContent.Trim());
            Assert.DoesNotContain("user-with-a-long-id", row.QuerySelector("td[data-label='Name']")?.TextContent);
        });

        await cut.InvokeAsync(() => cut.Find("button[aria-label='Copy user ID user-with-a-long-id']").Click());

        Assert.Equal("user-with-a-long-id", _clipboard.LastCopiedText);
    }

    [Fact]
    public void RoleList_RendersPermissionPreviewAndEmptyPermissionState()
    {
        _roles.Roles =
        [
            new() { Id = "admin", Name = "admin", Permissions = ["read:user", "update:user"] },
            new() { Id = "role-2", Name = "observer", Permissions = [] }
        ];

        var cut = Render<RolesPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("read:user", cut.Markup);
            Assert.Contains("No permissions", cut.Markup);
            Assert.Contains("Role ID", cut.Markup);
            Assert.Contains("Edit role admin", cut.Markup);
            Assert.Contains("Open role admin", cut.Markup);
            Assert.Equal(Breakpoint.Md, cut.FindComponent<MudTable<RoleSummary>>().Instance.Breakpoint);
            Assert.Empty(cut.FindAll("td[data-label='Name']")[0].QuerySelectorAll(".mud-typography-caption"));
        });
    }

    [Fact]
    public async Task RoleList_PresentsRoleIdSeparatelyAndCopiesTheFullValue()
    {
        _roles.Roles = [new() { Id = "workflow-manager", Name = "Workflow Manager", Permissions = ["read:user"] }];

        var cut = Render<RolesPage>();
        cut.WaitForAssertion(() =>
        {
            var row = cut.Find("tbody tr");
            Assert.Equal("Workflow Manager", row.QuerySelector("td[data-label='Name']")?.TextContent.Trim());
            Assert.Equal("workflow-manager", row.QuerySelector("td[data-label='Role ID'] .identity-id-value")?.TextContent.Trim());
            Assert.DoesNotContain("workflow-manager", row.QuerySelector("td[data-label='Name']")?.TextContent);
        });

        await cut.InvokeAsync(() => cut.Find("button[aria-label='Copy role ID workflow-manager']").Click());

        Assert.Equal("workflow-manager", _clipboard.LastCopiedText);
    }

    [Fact]
    public async Task CreateUser_UsesRoleIdsAndShowsGeneratedPasswordOnce()
    {
        _roles.Roles = [new() { Id = "power-user", Name = "Power User" }];
        _users.CreateResult = new() { Id = "user-3", Name = "carol", Password = "once-only", Roles = [] };
        var cut = Render<UserEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Create an Elsa account", cut.Markup));

        await cut.InvokeAsync(() => cut.FindComponent<MudSelect<string>>().Instance.SelectedValuesChanged.InvokeAsync(["power-user"]));
        cut.Find("input[type='text']").Change("carol");
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Create user").Click();

        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.Equal("carol", _users.CreateRequest?.Name);
            Assert.Null(_users.CreateRequest?.Password);
            Assert.Equal(["power-user"], _users.CreateRequest?.Roles);
            Assert.Contains("once-only", _dialogProvider.Markup);
            Assert.Contains("shown once", _dialogProvider.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public async Task PasswordOnlyUserUpdate_DoesNotResubmitUnchangedRoles()
    {
        _users.Users = [new() { Id = "user-1", Name = "alice", Roles = ["admin-role"] }];
        _roles.Roles = [new() { Id = "admin-role", Name = "Administrators" }];
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-1"));
        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));

        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[0].Change("replacement-password"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[1].Change("replacement-password"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Save changes").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("user-1", _users.UpdateId);
            Assert.Equal("replacement-password", _users.UpdateRequest?.Password);
            Assert.Null(_users.UpdateRequest?.Roles);
        });
    }

    [Fact]
    public async Task UserEditor_ReloadsAndClearsPasswordsWhenRouteIdChangesAfterCreate()
    {
        _users.CreateResult = new() { Id = "user-3", Name = "server-carol", Roles = [] };
        _users.Users = [new() { Id = "user-3", Name = "server-carol", Roles = [] }];
        var cut = Render<UserEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Create an Elsa account", cut.Markup));

        await cut.InvokeAsync(() => cut.Find("input[type='text']").Change("carol"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[0].Change("supplied-password"));
        await cut.InvokeAsync(() => cut.FindAll("input[type='password']")[1].Change("supplied-password"));
        await cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Create user").Click());
        cut.WaitForAssertion(() => Assert.Equal("carol", _users.CreateRequest?.Name));

        cut.Render(parameters => parameters.Add(component => component.Id, "user-3"));

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("input[readonly][value='server-carol']"));
            Assert.All(cut.FindAll("input[type='password']"), input => Assert.True(string.IsNullOrEmpty(input.GetAttribute("value"))));
        });
    }

    [Fact]
    public void UserEditor_IgnoresAStaleLoadAfterTheRouteChanges()
    {
        var firstLoad = new TaskCompletionSource<ListUsersResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondLoad = new TaskCompletionSource<ListUsersResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _users.ListResults.Enqueue(firstLoad.Task);
        _users.ListResults.Enqueue(secondLoad.Task);
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-a"));
        cut.WaitForState(() => _users.ListCallCount == 1);

        cut.Render(parameters => parameters.Add(component => component.Id, "user-b"));
        cut.WaitForState(() => _users.ListCallCount == 2);
        secondLoad.SetResult(new() { Users = [new() { Id = "user-b", Name = "bob" }] });
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("input[readonly][value='bob']")));

        firstLoad.SetResult(new() { Users = [new() { Id = "user-a", Name = "alice" }] });
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("input[readonly][value='bob']"));
            Assert.DoesNotContain("alice", cut.Markup);
        });
    }

    [Fact]
    public void CreateRole_SendsAddedPermissionTokens()
    {
        _roles.CreateResult = new() { Id = "role-3", Name = "auditor", Permissions = ["read:user"] };
        _roles.Roles = [_roles.CreateResult];
        var cut = Render<RoleEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Create a reusable set", cut.Markup));

        var inputs = cut.FindAll("input");
        inputs[0].Change("auditor");
        inputs[1].Input("read:user");
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Add").Click();
        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Create role").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("auditor", _roles.CreateRequest?.Name);
            Assert.Equal(["read:user"], _roles.CreateRequest?.Permissions);
        });

        cut.Render(parameters => parameters.Add(component => component.Id, "role-3"));
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("input[value='auditor']"));
            Assert.Contains("read:user", cut.Markup);
        });
    }

    [Fact]
    public void RoleEditor_IgnoresAStaleLoadAfterTheRouteChanges()
    {
        var firstLoad = new TaskCompletionSource<ListRolesResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var secondLoad = new TaskCompletionSource<ListRolesResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        _roles.ListResults.Enqueue(firstLoad.Task);
        _roles.ListResults.Enqueue(secondLoad.Task);
        var cut = Render<RoleEditor>(parameters => parameters.Add(component => component.Id, "role-a"));
        cut.WaitForState(() => _roles.ListCallCount == 1);

        cut.Render(parameters => parameters.Add(component => component.Id, "role-b"));
        cut.WaitForState(() => _roles.ListCallCount == 2);
        secondLoad.SetResult(new() { Roles = [new() { Id = "role-b", Name = "Operators", Permissions = ["read:user"] }] });
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("input[value='Operators']")));

        firstLoad.SetResult(new() { Roles = [new() { Id = "role-a", Name = "Administrators", Permissions = ["*"] }] });
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("input[value='Operators']"));
            Assert.DoesNotContain("Administrators", cut.Markup);
        });
    }

    [Fact]
    public void ReadOnlyLists_UseViewAffordancesAndHideMutations()
    {
        _permissionService.Set(IdentityPermissions.ReadUser, IdentityPermissions.ReadRole);
        _users.Users = [new() { Id = "user-1", Name = "alice" }];
        _roles.Roles = [new() { Id = "role-1", Name = "auditor" }];

        var users = Render<UsersPage>();
        var roles = Render<RolesPage>();

        users.WaitForAssertion(() =>
        {
            Assert.Contains("View user alice", users.Markup);
            Assert.DoesNotContain("Edit user alice", users.Markup);
            Assert.DoesNotContain("Create user", users.Markup);
            Assert.DoesNotContain("Delete user alice", users.Markup);
        });
        roles.WaitForAssertion(() =>
        {
            Assert.Contains("View role auditor", roles.Markup);
            Assert.DoesNotContain("Edit role auditor", roles.Markup);
            Assert.DoesNotContain("Create role", roles.Markup);
            Assert.DoesNotContain("Delete role auditor", roles.Markup);
        });
    }

    [Fact]
    public void UserDeletion_RequiresConfirmation()
    {
        _users.Users = [new() { Id = "user-1", Name = "alice" }];
        var cut = Render<UsersPage>();
        cut.WaitForAssertion(() => Assert.Contains("Delete user alice", cut.Markup));

        cut.Find("button[aria-label='Delete user alice']").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Delete user?", _dialogProvider.Markup));
        _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Cancel").Click();

        Assert.Empty(_users.DeletedIds);
    }

    [Fact]
    public async Task UserListDeletion_DisablesTheRowActionWhileTheRequestIsPending()
    {
        var deleteCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _users.DeleteHandler = _ => deleteCompletion.Task;
        _users.Users = [new() { Id = "user-1", Name = "alice" }];
        var cut = Render<UsersPage>();
        cut.WaitForAssertion(() => Assert.Contains("Delete user alice", cut.Markup));

        cut.Find("button[aria-label='Delete user alice']").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Delete user?", _dialogProvider.Markup));
        var confirmTask = _dialogProvider.InvokeAsync(() =>
            _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Delete").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, _users.DeleteCallCount);
            Assert.True(cut.Find("button[aria-label='Delete user alice']").HasAttribute("disabled"));
        });
        cut.Find("button[aria-label='Delete user alice']").Click();
        Assert.Equal(1, _users.DeleteCallCount);

        deleteCompletion.SetResult();
        await confirmTask;
    }

    [Fact]
    public void UserDeletion_IsCancelledWhenTheRouteChangesDuringConfirmation()
    {
        _users.Users =
        [
            new() { Id = "user-a", Name = "alice" },
            new() { Id = "user-b", Name = "bob" }
        ];
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-a"));
        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));

        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Delete user").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Delete alice?", _dialogProvider.Markup));
        cut.Render(parameters => parameters.Add(component => component.Id, "user-b"));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("input[readonly][value='bob']")));
        _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Delete").Click();

        Assert.Empty(_users.DeletedIds);
    }

    [Fact]
    public async Task UserDeletion_DisablesTheEditorWhileTheRequestIsPending()
    {
        var deleteCompletion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _users.DeleteHandler = _ => deleteCompletion.Task;
        _users.Users = [new() { Id = "user-a", Name = "alice" }];
        var cut = Render<UserEditor>(parameters => parameters.Add(component => component.Id, "user-a"));
        cut.WaitForAssertion(() => Assert.Contains("alice", cut.Markup));

        cut.FindAll("button").Single(x => x.TextContent.Trim() == "Delete user").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Delete alice?", _dialogProvider.Markup));
        var confirmTask = _dialogProvider.InvokeAsync(() =>
            _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Delete").Click());

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(1, _users.DeleteCallCount);
            Assert.True(cut.FindAll("button").Single(x => x.TextContent.Trim() == "Delete user").HasAttribute("disabled"));
        });
        deleteCompletion.SetResult();
        await confirmTask;
        Assert.Equal(["user-a"], _users.DeletedIds);
    }

    [Fact]
    public async Task GeneratedPasswordCompletion_DoesNotNavigateAfterEditorDisposal()
    {
        _users.CreateResult = new() { Id = "user-3", Name = "carol", Password = "once-only" };
        var navigation = Services.GetRequiredService<NavigationManager>();
        var cut = Render<UserEditor>();
        cut.WaitForAssertion(() => Assert.Contains("Create an Elsa account", cut.Markup));
        await cut.InvokeAsync(() => cut.Find("input[type='text']").Change("carol"));
        var createTask = cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Create user").Click());
        _dialogProvider.WaitForAssertion(() => Assert.Contains("once-only", _dialogProvider.Markup));
        var uriBeforeDisposal = navigation.Uri;

        Assert.IsAssignableFrom<IDisposable>(cut.Instance).Dispose();
        await _dialogProvider.InvokeAsync(() =>
            _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "I have saved it").Click());
        await createTask;
        cut.Dispose();

        Assert.Equal(uriBeforeDisposal, navigation.Uri);
    }

    [Fact]
    public async Task RoleSaveCompletion_DoesNotOverwriteANewRoute()
    {
        _roles.Roles =
        [
            new() { Id = "role-a", Name = "Administrators", Permissions = ["*"] },
            new() { Id = "role-b", Name = "Operators", Permissions = ["read:user"] }
        ];
        var updateCompletion = new TaskCompletionSource<RoleSummary>(TaskCreationOptions.RunContinuationsAsynchronously);
        _roles.UpdateHandler = (_, _) => updateCompletion.Task;
        var cut = Render<RoleEditor>(parameters => parameters.Add(component => component.Id, "role-a"));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("input[value='Administrators']")));
        await cut.InvokeAsync(() => cut.Find("input[value='Administrators']").Change("Admins"));

        var saveTask = cut.InvokeAsync(() => cut.FindAll("button").Single(x => x.TextContent.Trim() == "Save changes").Click());
        cut.WaitForState(() => _roles.UpdateId == "role-a");
        cut.Render(parameters => parameters.Add(component => component.Id, "role-b"));
        cut.WaitForAssertion(() => Assert.NotNull(cut.Find("input[value='Operators']")));
        updateCompletion.SetResult(new() { Id = "role-a", Name = "Admins", Permissions = ["*"] });
        await saveTask;

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(cut.Find("input[value='Operators']"));
            Assert.DoesNotContain("Admins", cut.Markup);
        });
    }

    private sealed class ApiProvider(UsersApi users, RolesApi roles) : IBackendApiClientProvider
    {
        public Uri Url => new("https://elsa.example.test");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            object api = typeof(T) == typeof(IUsersApi) ? users : roles;
            return new((T)api);
        }
    }

    private sealed class PermissionService(params string[] permissions) : IIdentityPermissionService
    {
        private IReadOnlySet<string> _permissions = permissions.ToHashSet(StringComparer.Ordinal);
        public void Set(params string[] values) => _permissions = values.ToHashSet(StringComparer.Ordinal);
        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) => new(_permissions.Contains("*") || _permissions.Contains(permission));
        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) => new(_permissions);
    }

    private sealed class Clipboard : IClipboard
    {
        public string? LastCopiedText { get; private set; }

        public Task CopyText(string text, CancellationToken cancellationToken = default)
        {
            LastCopiedText = text;
            return Task.CompletedTask;
        }
    }

    private sealed class UsersApi : IUsersApi
    {
        public ICollection<UserSummary> Users { get; set; } = [];
        public Queue<Task<ListUsersResponse>> ListResults { get; } = [];
        public int ListCallCount { get; private set; }
        public CreateUserRequest? CreateRequest { get; private set; }
        public CreateUserResponse CreateResult { get; set; } = new();
        public string? UpdateId { get; private set; }
        public UpdateUserRequest? UpdateRequest { get; private set; }
        public ICollection<string> DeletedIds { get; } = [];
        public int DeleteCallCount { get; private set; }
        public Func<string, Task>? DeleteHandler { get; set; }

        public Task<ListUsersResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            ListCallCount++;
            return ListResults.Count > 0 ? ListResults.Dequeue() : Task.FromResult(new ListUsersResponse { Users = Users });
        }
        public Task<CreateUserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequest = request;
            return Task.FromResult(CreateResult);
        }
        public Task<UserSummary> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default)
        {
            UpdateId = id;
            UpdateRequest = request;
            return Task.FromResult(Users.Single(user => user.Id == id));
        }
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;
            DeletedIds.Add(id);
            return DeleteHandler?.Invoke(id) ?? Task.CompletedTask;
        }
    }

    private sealed class RolesApi : IRolesApi
    {
        public ICollection<RoleSummary> Roles { get; set; } = [];
        public Queue<Task<ListRolesResponse>> ListResults { get; } = [];
        public int ListCallCount { get; private set; }
        public CreateRoleRequest? CreateRequest { get; private set; }
        public RoleSummary CreateResult { get; set; } = new();
        public string? UpdateId { get; private set; }
        public UpdateRoleRequest? UpdateRequest { get; private set; }
        public Func<string, UpdateRoleRequest, Task<RoleSummary>>? UpdateHandler { get; set; }

        public Task<ListRolesResponse> ListAsync(CancellationToken cancellationToken = default)
        {
            ListCallCount++;
            return ListResults.Count > 0 ? ListResults.Dequeue() : Task.FromResult(new ListRolesResponse { Roles = Roles });
        }
        public Task<RoleSummary> CreateAsync(CreateRoleRequest request, CancellationToken cancellationToken = default)
        {
            CreateRequest = request;
            return Task.FromResult(CreateResult);
        }
        public Task<RoleSummary> UpdateAsync(string id, UpdateRoleRequest request, CancellationToken cancellationToken = default)
        {
            UpdateId = id;
            UpdateRequest = request;
            return UpdateHandler?.Invoke(id, request) ?? Task.FromResult(Roles.Single(role => role.Id == id));
        }
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
