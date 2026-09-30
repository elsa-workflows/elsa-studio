using Bunit;
using Elsa.Studio.Authorization;
using Elsa.Studio.Components;
using Elsa.Studio.Contracts;
using Elsa.Studio.Dashboard.Client;
using Elsa.Studio.Dashboard.Components;
using Elsa.Studio.Dashboard.Menu;
using Elsa.Studio.Dashboard.Models;
using Elsa.Studio.Dashboard.Services;
using Elsa.Studio.Dashboard.Widgets;
using Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.UI.Dashboard;
using Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.UI.Dashboard;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Dashboard.Widgets;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using DashboardPage = Elsa.Studio.Dashboard.Pages.Index;

namespace Elsa.Studio.Dashboard.Tests;

/// <summary>
/// Every signed-in user can open the dashboard. It shows a user only the widgets they may see, requests only the data those
/// widgets need from endpoints the user may call, and welcomes a user who may see none with shortcuts instead.
/// </summary>
public sealed class DashboardPagePermissionTests : BunitContext, IAsyncLifetime
{
    private const string OverviewEndpoint = "overview";
    private static readonly string[] EveryEndpoint = ["needs-attention", OverviewEndpoint, "recent-activity", "workflow-hotspots", "workflow-trends"];
    private static readonly Type[] WorkflowInstanceWidgets = [typeof(DashboardWorkflowMetricsWidget), typeof(DashboardNeedsAttentionWidget), typeof(DashboardTrendWidget), typeof(DashboardRecentActivityWidget), typeof(DashboardWorkflowHotspotsWidget)];
    private static readonly Type[] DiagnosticsWidgets = [typeof(StructuredLogsDashboardWidget), typeof(ConsoleLogsDashboardWidget)];

    private readonly RecordingDashboardApi _api = new();
    private readonly StubFeatureService _features = new();
    private readonly DashboardWidgetRegistry _registry = new();
    private readonly StubPermissionService _permissions = new(UserPermissions.Unknown);

    public DashboardPagePermissionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IDashboardService>(new DashboardService(new StubBackend(_api)));
        Services.AddSingleton<IDashboardWidgetRegistry>(_registry);
        Services.AddSingleton<IEnumerable<DashboardWidgetDescriptor>>([]);
        Services.AddSingleton<IFeatureService>(_features);
        Services.AddSingleton<IPermissionService>(_permissions);
        Services.AddSingleton<IMenuService>(new DefaultMenuService([new DashboardMenu(new TestLocalizer()), new NavigationMenu()], [new DefaultMenuGroupProvider()], _permissions));
        Render<MudPopoverProvider>();
    }

    [Fact]
    public async Task AUserWhoCanViewWorkflowInstances_SeesTheInstanceWidgetsOnly_AndLoadsNothingElse()
    {
        var cut = await RenderDashboardAsync("workflows/instances:view");

        cut.WaitForAssertion(() => Assert.Equal(Names(WorkflowInstanceWidgets), ShownWidgets(cut)));
        Assert.Empty(cut.FindComponents<DashboardRuntimeChip>());
        Assert.Equal(EveryEndpoint, _api.Calls.Order());
    }

    [Theory]
    [InlineData("diagnostics/structured-logs:view", typeof(StructuredLogsDashboardWidget))]
    [InlineData("diagnostics/console-logs:view", typeof(ConsoleLogsDashboardWidget))]
    public async Task AUserWhoCanViewOneLogSummary_SeesItsWidget_AndRequestsOnlyTheOverview(string grant, Type widget)
    {
        var cut = await RenderDashboardAsync(grant);

        cut.WaitForAssertion(() => Assert.Equal(Names(widget), ShownWidgets(cut)));
        Assert.Equal([OverviewEndpoint], _api.Calls);
    }

    [Theory]
    [InlineData("dashboard:view")]
    [InlineData(null)]
    public async Task ADashboardViewer_AndAUserWhosePermissionsAreUnknown_SeeEverything(string? grant)
    {
        var cut = await RenderDashboardAsync(grant == null ? UserPermissions.Unknown : StubPermissionService.Grants(grant));

        cut.WaitForAssertion(() => Assert.Equal(Names([.. WorkflowInstanceWidgets, .. DiagnosticsWidgets]), ShownWidgets(cut)));
        Assert.Single(cut.FindComponents<DashboardRuntimeChip>());
        Assert.Equal(EveryEndpoint, _api.Calls.Order());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task TheRuntimeStatus_ShowsOnlyToUsersWhoMayReadIt(bool canViewRuntime)
    {
        string[] grants = canViewRuntime ? ["workflows/instances:view", "workflows/runtime:view"] : ["workflows/instances:view"];

        var cut = await RenderDashboardAsync(grants);

        cut.WaitForAssertion(() => Assert.NotEmpty(ShownWidgets(cut)));
        Assert.Equal(canViewRuntime, cut.FindComponents<DashboardRuntimeChip>().Count == 1);
    }

    [Fact]
    public async Task ASectionTheBackendWithholds_IsLeftOut_RatherThanReported()
    {
        _api.Overview = new()
        {
            Runtime = new() { Capability = DashboardCapabilityStatus.Unauthorized },
            WorkflowInstances = new() { Capability = DashboardCapabilityStatus.Unauthorized },
            Diagnostics = new()
            {
                StructuredLogs = new() { Capability = DashboardCapabilityStatus.Unauthorized },
                ConsoleLogs = new() { Capability = DashboardCapabilityStatus.Unauthorized }
            }
        };
        _api.NeedsAttention = new() { Capability = DashboardCapabilityStatus.Unauthorized };

        var cut = await RenderDashboardAsync(UserPermissions.Unknown);

        cut.WaitForAssertion(() => Assert.Equal(Names(typeof(DashboardTrendWidget), typeof(DashboardRecentActivityWidget), typeof(DashboardWorkflowHotspotsWidget)), ShownWidgets(cut)));
        Assert.Empty(cut.FindComponents<DashboardRuntimeChip>());
        Assert.DoesNotContain(DashboardUiMapper.CapabilityLabel(DashboardCapabilityStatus.Unauthorized), cut.Markup);
    }

    [Fact]
    public async Task WithoutAWidgetToShow_TheWelcomePanelLinksToThePagesTheUserCanOpen_InNavigationOrder()
    {
        var cut = await RenderDashboardAsync("secrets:view", "workflows/definitions:view", "workflows/runtime:view");

        // Secrets is listed first but belongs to a later group; the dashboard itself is not a shortcut.
        cut.WaitForAssertion(() => Assert.Equal(["workflows/definitions", "security/secrets"], cut.FindAll("[data-testid='dashboard-welcome'] a").Select(x => x.GetAttribute("href"))));
        Assert.Empty(_api.Calls);
        Assert.Empty(cut.FindAll("[data-testid='access-denied']"));
        Assert.Equal("http://localhost/", Services.GetRequiredService<NavigationManager>().Uri);
    }

    [Fact]
    public async Task WithoutAWidgetOrAPageToOpen_TheNoPagesNoticeIsShown()
    {
        var cut = await RenderDashboardAsync("unrelated:view");

        cut.WaitForAssertion(() => Assert.NotEmpty(cut.FindAll("[data-testid='no-accessible-pages']")));
        Assert.Empty(cut.FindAll("[data-testid='dashboard-welcome']"));
        Assert.Empty(_api.Calls);
    }

    [Theory]
    [InlineData("dashboard:view", false)]
    [InlineData("secrets:view", true)]
    public async Task WhileTheWidgetsAreStillBeingRegistered_TheDashboardIsLoading_NotWelcoming(string grant, bool welcomed)
    {
        var cut = RenderDashboard(StubPermissionService.Grants(grant));

        Assert.Contains("Loading dashboard", cut.Find(".mud-chip").TextContent);
        Assert.Empty(cut.FindAll("[data-testid='dashboard-welcome']"));
        Assert.Empty(_api.Calls);

        await InitializeFeaturesAsync();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(welcomed, cut.FindAll("[data-testid='dashboard-welcome']").Count == 1);
            Assert.Equal(!welcomed, ShownWidgets(cut).Count > 0);
            Assert.Equal(!welcomed, _api.Calls.Count > 0);
        });
    }

    [Fact]
    public async Task TheDashboard_IsNotGated()
    {
        Assert.Empty(RequirePermissionAttribute.GetRequiredPermissions(typeof(DashboardPage)));
        Assert.All(await new DashboardMenu(new TestLocalizer()).GetMenuItemsAsync(), item => Assert.Empty(item.RequiredPermissions));
    }

    [Theory]
    [InlineData("acme/things:view", true)]
    [InlineData("secrets:view", false)]
    public async Task AContributedWidget_ShowsOnlyToUsersHoldingThePermissionItDeclares(string grant, bool shown)
    {
        _registry.Add(new("acme.things", DashboardWidgetZones.SecondaryPanels, 10, typeof(AcmeWidget)) { RequiredPermissions = [new("acme/things", PermissionVerbs.View)] });

        var cut = await RenderDashboardAsync(grant);

        IReadOnlyList<string> expectedWidgets = shown ? Names(typeof(AcmeWidget)) : [];
        string[] expectedCalls = shown ? [OverviewEndpoint] : [];
        cut.WaitForAssertion(() => Assert.Equal(expectedWidgets, ShownWidgets(cut)));
        Assert.Equal(expectedCalls, _api.Calls);
    }

    [Fact]
    public async Task AContributedWidgetDeclaringNoPermission_ShowsToEveryone()
    {
        _registry.Add(new("acme.status", DashboardWidgetZones.SecondaryPanels, 10, typeof(AcmeWidget)));

        var cut = await RenderDashboardAsync("secrets:view");

        cut.WaitForAssertion(() => Assert.Equal(Names(typeof(AcmeWidget)), ShownWidgets(cut)));
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync() => await base.DisposeAsync();

    private Task<IRenderedComponent<PermissionPageGuard>> RenderDashboardAsync(params string[] grants) =>
        RenderDashboardAsync(StubPermissionService.Grants(grants));

    private async Task<IRenderedComponent<PermissionPageGuard>> RenderDashboardAsync(UserPermissions permissions)
    {
        await InitializeFeaturesAsync();
        return RenderDashboard(permissions);
    }

    // Rendered the way the shell renders a page: inside the guard that enforces its declared permissions.
    private IRenderedComponent<PermissionPageGuard> RenderDashboard(UserPermissions permissions)
    {
        _permissions.Permissions = permissions;

        return Render<PermissionPageGuard>(parameters => parameters
            .AddCascadingValue(new RouteData(typeof(DashboardPage), new Dictionary<string, object?>()))
            .AddChildContent<DashboardPage>());
    }

    // As the shell does once the user has signed in: the dashboard companions register their widgets, then the feature
    // service reports it is done.
    private async Task InitializeFeaturesAsync()
    {
        IFeature[] companions =
        [
            new Elsa.Studio.Workflows.Dashboard.Feature(_registry),
            new Elsa.Studio.Diagnostics.StructuredLogs.Dashboard.Feature(_registry),
            new Elsa.Studio.Diagnostics.ConsoleLogs.Dashboard.Feature(_registry)
        ];

        foreach (var companion in companions)
            await companion.InitializeAsync();

        _features.CompleteInitialization();
    }

    // The widgets that rendered anything: a widget whose data the backend withholds renders nothing.
    private static IReadOnlyList<string> ShownWidgets(IRenderedComponent<PermissionPageGuard> cut) => cut
        .FindComponents<DynamicComponent>()
        .Where(x => !string.IsNullOrWhiteSpace(x.Markup))
        .Select(x => x.Instance.Type.Name)
        .Order()
        .ToList();

    private static IReadOnlyList<string> Names(params Type[] widgets) => widgets.Select(x => x.Name).Order().ToList();

    private sealed class AcmeWidget : ComponentBase
    {
        [Parameter] public DashboardWidgetContext Context { get; set; } = null!;
        [Parameter] public DashboardWidgetDescriptor Descriptor { get; set; } = null!;

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder) => builder.AddContent(0, "Acme");
    }

    private sealed class RecordingDashboardApi : IDashboardApi
    {
        public List<string> Calls { get; } = [];
        public DashboardOverview Overview { get; set; } = new();
        public DashboardNeedsAttentionResponse NeedsAttention { get; set; } = new();

        public Task<DashboardOverview> GetOverviewAsync(string? range = null, bool includeSystem = false, CancellationToken cancellationToken = default) => Record(OverviewEndpoint, Overview);
        public Task<DashboardTrendResponse> GetWorkflowTrendsAsync(DashboardTrendRequest request, CancellationToken cancellationToken = default) => Record("workflow-trends", new DashboardTrendResponse());
        public Task<DashboardNeedsAttentionResponse> GetNeedsAttentionAsync(string? range = null, int take = 8, bool includeSystem = false, CancellationToken cancellationToken = default) => Record("needs-attention", NeedsAttention);
        public Task<DashboardRecentActivityResponse> GetRecentActivityAsync(string? range = null, int take = 20, bool includeSystem = false, CancellationToken cancellationToken = default) => Record("recent-activity", new DashboardRecentActivityResponse());
        public Task<DashboardWorkflowHotspotsResponse> GetWorkflowHotspotsAsync(DashboardWorkflowHotspotsRequest request, CancellationToken cancellationToken = default) => Record("workflow-hotspots", new DashboardWorkflowHotspotsResponse());

        private Task<T> Record<T>(string endpoint, T response)
        {
            Calls.Add(endpoint);
            return Task.FromResult(response);
        }
    }

    private sealed class StubBackend(IDashboardApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://elsa.example.test/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class => ValueTask.FromResult((T)api);
    }

    private sealed class StubFeatureService : IFeatureService
    {
        public event Action? Initialized;
        public bool IsInitialized { get; private set; }
        public IEnumerable<IFeature> GetFeatures() => [];
        public Task InitializeFeaturesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public void CompleteInitialization()
        {
            IsInitialized = true;
            Initialized?.Invoke();
        }
    }

    // Navigation shaped like the built-in modules': Secrets is listed first but belongs to a later group.
    private sealed class NavigationMenu : IMenuProvider
    {
        public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default) => new(new[]
        {
            Item("Secrets", "security/secrets", MenuItemGroups.Administration, "secrets"),
            new MenuItem
            {
                Text = "Workflows", Href = "", GroupName = MenuItemGroups.General.Name, Order = 10,
                SubMenuItems = [Item("Definitions", "workflows/definitions", MenuItemGroups.General, "workflows/definitions")]
            }
        });

        private static MenuItem Item(string text, string href, MenuItemGroup group, string resource) => new()
        {
            Text = text, Href = href, GroupName = group.Name, RequiredPermissions = [new(resource, PermissionVerbs.View)]
        };
    }
}
