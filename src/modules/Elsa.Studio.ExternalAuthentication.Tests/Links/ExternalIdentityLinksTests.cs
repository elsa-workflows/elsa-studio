using Bunit;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Menu;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using IdentityLinksPage = Elsa.Studio.ExternalAuthentication.Pages.IdentityLinks.Index;

namespace Elsa.Studio.ExternalAuthentication.Tests.Links;

public sealed class ExternalIdentityLinksTests : BunitContext, IAsyncLifetime
{
    private readonly LinksApi _links = new();
    private readonly ConnectionsApi _connections = new();

    public ExternalIdentityLinksTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<IBackendApiClientProvider>(new ApiProvider(_links, _connections));
        Services.AddSingleton<IExternalAuthenticationPermissionService>(
            new PermissionService(ExternalAuthenticationPermissions.ManageLinks));
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void LinkPageShowsOnlySafeTupleMetadataAndAOneTimeSubjectInput()
    {
        _links.ListResults.Enqueue(new(
        [
            new ExternalIdentityLink(
                "link-1",
                "user-1",
                "connection-1",
                "https://login.contoso.example",
                "00u…cdef",
                DateTimeOffset.UtcNow,
                null)
        ], null));
        _links.Users = [new("user-1", "workflow-admin")];
        _connections.Result = new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Id = "connection-1", Key = "contoso", DisplayName = "Contoso" }]
        };

        var cut = Render<IdentityLinksPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("workflow-admin", cut.Markup);
            Assert.Contains("Contoso", cut.Markup);
            Assert.Contains("https://login.contoso.example", cut.Markup);
            Assert.Contains("00u…cdef", cut.Markup);
            Assert.Contains("type=\"password\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("never returned", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void LinkListUsesServerCursorAndTenantScopedFilters()
    {
        _links.Users = [];
        _connections.Result = new();
        _links.ListResults.Enqueue(new([], "cursor-2"));
        _links.ListResults.Enqueue(new([], null));

        var cut = Render<IdentityLinksPage>();
        cut.WaitForAssertion(() => Assert.Contains("Next page", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Next page", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal([null, "cursor-2"], _links.Cursors));
    }

    [Fact]
    public async Task SecurityMenuShowsLinksOnlyWithTheDedicatedPermission()
    {
        var menu = new ExternalAuthenticationSecurityMenuContributor(
            new FeatureProvider(),
            new PermissionService(ExternalAuthenticationPermissions.ManageLinks));

        var item = Assert.Single(await menu.GetMenuItemsAsync());

        Assert.Equal("security/external-authentication/identity-links", item.Href);
        Assert.DoesNotContain("connections", item.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PrelinkValidationRequiresAnHttpsIssuerAndNeverNeedsRoleOrPermissionData()
    {
        var request = new PrelinkExternalIdentityRequest
        {
            UserId = "user-1",
            ConnectionId = "connection-1",
            Issuer = "http://insecure.example",
            Subject = "subject-1"
        };

        var errors = IdentityLinkUiState.Validate(request);

        Assert.Single(errors);
        Assert.Contains("HTTPS", errors.Single(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(errors, error => error.Contains("role", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(errors, error => error.Contains("permission", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class ApiProvider(LinksApi links, ConnectionsApi connections) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://elsa.example.test/elsa/api/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            object api = typeof(T) == typeof(IExternalIdentityLinksApi) ? links :
                typeof(T) == typeof(IExternalAuthenticationConnectionsApi) ? connections :
                throw new NotSupportedException(typeof(T).FullName);
            return ValueTask.FromResult((T)api);
        }
    }

    private sealed class LinksApi : IExternalIdentityLinksApi
    {
        public Queue<ListExternalIdentityLinksResponse> ListResults { get; } = new();
        public IReadOnlyCollection<IdentityLinkUser> Users { get; set; } = [];
        public List<string?> Cursors { get; } = [];

        public Task<ListExternalIdentityLinksResponse> ListAsync(string? userId = null, string? connectionId = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default)
        {
            Cursors.Add(cursor);
            return Task.FromResult(ListResults.Dequeue());
        }

        public Task<FindIdentityLinkUsersResponse> FindUsersAsync(string? search = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FindIdentityLinkUsersResponse(Users, null));

        public Task<ExternalIdentityLink> PrelinkAsync(PrelinkExternalIdentityRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UnlinkAsync(string linkId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ConnectionsApi : IExternalAuthenticationConnectionsApi
    {
        public ListConnectionsResponse Result { get; set; } = new();
        public Task<ListConnectionsResponse> ListAsync(string? search = null, string? source = null, string? scope = null, string? adapterType = null, bool? enabled = null, bool? valid = null, bool? shadowed = null, bool? archived = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default) => Task.FromResult(Result);
        public Task<ConnectionDetail> GetAsync(string connectionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<AdapterDescriptor>> GetAdaptersAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<PermissionGrantSourceDescriptor>> GetPermissionSourcesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<UnlinkedIdentityPolicyDescriptor>> GetPoliciesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<ExternalUserMatcherDescriptor>> GetUserMatchersAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ManagedSecretResolverCatalog> GetManagedSecretResolversAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<PermissionDescriptor>> GetPermissionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionDetail> CreateAsync(ConnectionMutation request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionDetail> UpdateAsync(string connectionId, ConnectionMutation request, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task EnableAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DisableAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ArchiveAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RestoreAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionValidationResult> ValidateAsync(string connectionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ReplaceSecretBindingAsync(string connectionId, string fieldName, SecretBindingMutation request, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionDetail> ReplaceManagedSecretAsync(string connectionId, string fieldName, ManagedSecretMutation request, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveSecretBindingAsync(string connectionId, string fieldName, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class PermissionService(params string[] permissions) : IExternalAuthenticationPermissionService
    {
        private readonly IReadOnlySet<string> _permissions = permissions.ToHashSet(StringComparer.Ordinal);
        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) => ValueTask.FromResult(_permissions.Contains(permission));
        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(_permissions);
    }

    private sealed class FeatureProvider : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }
}
