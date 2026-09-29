using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Testing;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.VersionHistory;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.Tests.Support;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// The instance designer, the version history and the activity properties panel only offer the actions the user's
/// permissions allow. The workflow editor's own toolbar is covered by <see cref="WorkflowEditorLifecycleTests"/>.
/// </summary>
public sealed class WorkflowActionPermissionTests : BunitContext, IAsyncLifetime
{
    private const string ViewOnly = "workflows/definitions:view";
    private readonly IRenderedComponent<MudPopoverProvider> _popovers;

    public WorkflowActionPermissionTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddRemoteBackend();
        Services.AddWorkflowsModule();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IActivityRegistry>(new TestActivityRegistry([]));
        Services.AddSingleton<IWorkflowDefinitionService, VersionsWorkflowDefinitionService>();
        Services.AddSingleton<IRemoteFeatureProvider, EnabledRemoteFeatureProvider>();
        Services.AddSingleton<IExpressionService, NoExpressionService>();
        Services.AddSingleton<IWorkflowInstanceObserverFactory, UnusedObserverFactory>();
        ComponentFactories.Add<DiagramDesignerWrapper, TestDiagramDesignerWrapper>();
        _popovers = Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Theory]
    [InlineData(new[] { ViewOnly, "workflows/instances:view" }, false)]
    [InlineData(new[] { ViewOnly, "workflows/instances:view", "alterations:execute" }, true)]
    public void InstanceDesigner_OffersAlterOnlyWithTheAlterPermission(string[] grants, bool canAlter)
    {
        var instance = new WorkflowInstance { Id = "instance-1", DefinitionId = "definition-1", Status = WorkflowStatus.Running };
        var cut = Render<RunningInstanceDesigner>(parameters => parameters
            .Add(x => x.WorkflowDefinition, new WorkflowDefinition { Id = "version-1", DefinitionId = "definition-1" })
            .Add(x => x.WorkflowInstance, instance)
            .AddCascadingValue(StubPermissionService.Grants(grants)));

        var actions = cut.FindComponents<MudIconButton>().Select(x => x.Instance.Icon).ToList();
        Assert.Contains(Icons.Material.Outlined.EditNote, actions);
        Assert.Equal(canAlter, actions.Contains(Icons.Material.Outlined.Tune));
    }

    [Theory]
    [InlineData(new[] { ViewOnly }, false, false)]
    [InlineData(new[] { ViewOnly, "workflows/definitions/versions:revert" }, true, false)]
    [InlineData(new[] { ViewOnly, "workflows/definitions:delete" }, false, true)]
    public void VersionHistory_OffersRollbackAndDeleteOnlyWithTheirPermissions(string[] grants, bool canRollback, bool canDelete)
    {
        var cut = Render<VersionHistoryTab>(parameters => parameters
            .Add(x => x.DefinitionId, "definition-1")
            .AddCascadingValue(new WorkflowDefinitionWorkspace())
            .AddCascadingValue(StubPermissionService.Grants(grants)));

        cut.WaitForElement("tbody .mud-menu button").Click();

        Assert.Contains("View", _popovers.Markup);
        Assert.Equal(canRollback, _popovers.Markup.Contains("Rollback to this version"));
        Assert.Equal(canDelete, _popovers.Markup.Contains("Delete"));
        Assert.Equal(canDelete, cut.Markup.Contains("Bulk actions"));
    }

    [Theory]
    [InlineData(new[] { ViewOnly }, false)]
    [InlineData(new[] { ViewOnly, "workflows/tests:execute" }, true)]
    public void ActivityProperties_OffersTheTestTabOnlyWithTheTestPermission(string[] grants, bool canTest)
    {
        var cut = Render<ActivityPropertiesPanel>(parameters => parameters.AddCascadingValue(StubPermissionService.Grants(grants)));

        var tabs = cut.WaitForElements(".mud-tab").Select(x => x.TextContent.Trim()).ToList();
        Assert.Contains("Common", tabs);
        Assert.Equal(canTest, tabs.Contains("Test"));
    }

    /// <summary>A <see cref="WorkflowInstanceDesigner"/> that keeps its markup but skips the first-render designer setup.</summary>
    private sealed class RunningInstanceDesigner : WorkflowInstanceDesigner
    {
        protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;
    }

    private sealed class VersionsWorkflowDefinitionService : ThrowingWorkflowDefinitionServiceBase
    {
        public override Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedListResponse<WorkflowDefinitionSummary>
            {
                Items = [new() { Id = "definition-1:2", DefinitionId = "definition-1", Version = 2, IsLatest = true }, new() { Id = "definition-1:1", DefinitionId = "definition-1", Version = 1 }],
                TotalCount = 2
            });
    }

    private sealed class NoExpressionService : IExpressionService
    {
        public Task<IEnumerable<ExpressionDescriptor>> ListDescriptorsAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<ExpressionDescriptor>>([]);
        public Task<ExpressionDescriptor?> GetByTypeAsync(string type, CancellationToken cancellationToken = default) => Task.FromResult<ExpressionDescriptor?>(null);
    }

    // The designer only creates an observer once its diagram is up, which these tests never render.
    private sealed class UnusedObserverFactory : IWorkflowInstanceObserverFactory
    {
        public Task<IWorkflowInstanceObserver> CreateAsync(string workflowInstanceId) => throw new NotSupportedException();
        public Task<IWorkflowInstanceObserver> CreateAsync(WorkflowInstanceObserverContext context) => throw new NotSupportedException();
    }

    private sealed class EnabledRemoteFeatureProvider : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
