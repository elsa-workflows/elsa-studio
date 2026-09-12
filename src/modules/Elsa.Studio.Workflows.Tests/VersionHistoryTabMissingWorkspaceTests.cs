using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.VersionHistory;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Pins that <see cref="VersionHistoryTab"/> survives being rendered without a cascading
/// <c>WorkflowDefinitionWorkspace</c>.
/// <para>
/// The tab takes its workspace as a cascading parameter of a concrete type, so any host that cascades a
/// workspace of a different type - a downstream copy of <c>WorkflowDefinitionWorkspace</c> under its own
/// namespace, for instance - leaves the parameter null. Dereferencing it unconditionally in
/// <c>OnInitialized</c> throws once into an error boundary, and then again from
/// <see cref="IDisposable.Dispose"/> during the disposal batch of the renderer, where nothing can catch
/// it: that second throw takes down the whole Blazor circuit rather than just this tab.
/// </para>
/// </summary>
public sealed class VersionHistoryTabMissingWorkspaceTests : BunitContext, IAsyncLifetime
{
    private readonly VersionListingWorkflowDefinitionService _definitionService = new();

    public VersionHistoryTabMissingWorkspaceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IWorkflowDefinitionService>(_definitionService);
        Services.AddSingleton<IWorkflowDefinitionHistoryService>(new UnusedWorkflowDefinitionHistoryService());
        Services.AddSingleton<IMediator>(new UnusedMediator());
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void RendersTheVersionListWithoutACascadingWorkspace()
    {
        var cut = RenderTab();

        cut.WaitForAssertion(() => Assert.Contains("demo-inzorg-client", _definitionService.RequestedDefinitionIds));
        Assert.Contains("Version history", cut.Markup);
    }

    [Fact]
    public void DisposalWithoutACascadingWorkspaceDoesNotThrow()
    {
        var cut = RenderTab();

        var exception = Record.Exception(() => ((IDisposable)cut.Instance).Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void EditActionsAreDisabledWithoutACascadingWorkspace()
    {
        var cut = RenderTab();

        cut.WaitForAssertion(() => Assert.Contains(cut.FindComponents<MudMenu>(), menu => menu.Instance.Label == "Bulk actions"));
        var bulkActions = cut.FindComponents<MudMenu>().Single(menu => menu.Instance.Label == "Bulk actions");
        Assert.True(bulkActions.Instance.Disabled);
    }

    private IRenderedComponent<VersionHistoryTab> RenderTab() => Render<VersionHistoryTab>(parameters => parameters
        .Add(x => x.DefinitionId, "demo-inzorg-client"));

    private sealed class VersionListingWorkflowDefinitionService : IWorkflowDefinitionService
    {
        public ICollection<string> RequestedDefinitionIds { get; } = [];

        public Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default)
        {
            foreach (var definitionId in request.DefinitionIds ?? [])
                RequestedDefinitionIds.Add(definitionId);

            return Task.FromResult(new PagedListResponse<WorkflowDefinitionSummary>
            {
                Items = [new WorkflowDefinitionSummary { Id = "v1", DefinitionId = "demo-inzorg-client", Version = 1, IsLatest = true }],
                TotalCount = 1
            });
        }

        public Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowDefinition?> FindByIdAsync(string id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IEnumerable<WorkflowDefinition>> FindManyByIdAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ActivityNode?> FindSubgraphAsync(string id, string? parentNodeId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GetPathSegmentsResponse?> GetPathSegmentsAsync(string id, string? childNodeId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DeleteVersionAsync(WorkflowDefinitionVersion workflowDefinitionVersion, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SaveWorkflowDefinitionResponse> PublishAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<long> BulkDeleteAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<long> BulkDeleteVersionsAsync(IEnumerable<WorkflowDefinitionVersion> workflowDefinitionVersions, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BulkPublishWorkflowDefinitionsResponse> BulkPublishAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<BulkRetractWorkflowDefinitionsResponse> BulkRetractAsync(IEnumerable<string> definitionIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description = null, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description, string? rootActivityTemplateKey, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> ExportDefinitionAsync(string definitionId, VersionOptions? versionOptions = null, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> BulkExportDefinitionsAsync(IEnumerable<string> ids, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UpdateConsumingWorkflowReferencesResponse> UpdateReferencesAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ExecuteWorkflowResult> ExecuteAsync(string definitionId, ExecuteWorkflowDefinitionRequest? request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnusedWorkflowDefinitionHistoryService : IWorkflowDefinitionHistoryService
    {
        public Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowRetractedCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkflowDefinitionSummary> RevertAsync(WorkflowDefinitionVersion workflowDefinitionVersion, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class UnusedMediator : IMediator
    {
        public void Subscribe<TNotification, THandler>(THandler handler) where THandler : INotificationHandler<TNotification> where TNotification : INotification => throw new NotSupportedException();
        public void Unsubscribe<TNotification, THandler>(THandler handler) where THandler : INotificationHandler<TNotification> where TNotification : INotification => throw new NotSupportedException();
        public void Unsubscribe(INotificationHandler handler) => throw new NotSupportedException();
        public Task NotifyAsync<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => throw new NotSupportedException();
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
