using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Components;
using Elsa.Studio.Security.Models;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Security.Tests;

public sealed class UsersPageTests : BunitContext, IAsyncLifetime
{
    public UsersPageTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IClipboard>(new NoOpClipboard());
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    // Core only returns users of the caller's tenant, so a tenant label or column would repeat the same value on every row.
    [Fact]
    public void Render_WhenUsersAreLoaded_ShowsTheListWithoutTenantUi()
    {
        var cut = RenderUsers(new UserSummary { Id = "user-1", Name = "admin", Roles = ["admin"] });

        cut.WaitForAssertion(() => Assert.Contains("1 user · all loaded", cut.Markup));
        Assert.DoesNotContain("Host", cut.Markup);
        Assert.DoesNotContain("Scope", cut.Markup);
        Assert.DoesNotContain("Tenant", cut.Markup);
    }

    private IRenderedComponent<UserListSurface> RenderUsers(params UserSummary[] users)
    {
        Services.AddSingleton<IBackendApiClientProvider>(new StaticBackendApiClientProvider(new StubUsersApi(users)));
        Render<MudPopoverProvider>();
        return Render<UserListSurface>(parameters => parameters
            .Add(x => x.Access, new UserAdministrationAccess(UserAdministrationAccessState.Ready, true, true, true, true)));
    }

    private sealed class NoOpClipboard : IClipboard
    {
        public Task CopyText(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class StubUsersApi(params UserSummary[] users) : IUsersApi
    {
        public Task<ListUsersResponse> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult(new ListUsersResponse { Users = users });
        public Task<CreateUserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UserSummary> UpdateAsync(string id, UpdateUserRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DeleteAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
