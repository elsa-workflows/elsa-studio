using Bunit;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class ExecutionDetailsDrawerRenderTests : BunitContext, IAsyncLifetime
{
    public ExecutionDetailsDrawerRenderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IClipboard, ClipboardStub>();
        Services.AddSingleton<IActivityExecutionService, ActivityExecutionServiceStub>();
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void TemporaryDrawerMountedOpen_RendersExecutionDetails()
    {
        var record = new ActivityExecutionRecord
        {
            Id = "exec-1",
            ActivityId = "WriteLine1",
            ActivityNodeId = "Workflow1:WriteLine1",
            ActivityType = "Elsa.WriteLine",
            Status = ActivityStatus.Completed,
            StartedAt = DateTimeOffset.UtcNow,
            ActivityState = new Dictionary<string, object?> { ["Text"] = "hello" },
            Payload = new Dictionary<string, object?> { ["Outcomes"] = "Done" },
            Outputs = new Dictionary<string, object?> { ["Result"] = "ok" }
        };

        var cut = Render<ExecutionDetailsDrawerHarness>(parameters => parameters
            .Add(harness => harness.ActivityExecution, record));

        var drawer = cut.Find("aside.execution-details-drawer");
        Assert.Contains("mud-drawer--open", drawer.ClassList);
        Assert.Contains("mud-drawer-temporary", drawer.ClassList);
        Assert.Contains("Execution Details", cut.Markup);
        Assert.Contains("hello", cut.Markup);
        Assert.Contains("Done", cut.Markup);
        Assert.Contains("ok", cut.Markup);
    }

    private sealed class ExecutionDetailsDrawerHarness : ComponentBase
    {
        [Parameter] public ActivityExecutionRecord? ActivityExecution { get; set; }

        protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
        {
            builder.OpenComponent<MudDrawer>(0);
            builder.AddAttribute(1, "Open", true);
            builder.AddAttribute(2, "Anchor", Anchor.End);
            builder.AddAttribute(3, "Width", "600px");
            builder.AddAttribute(4, "Elevation", 2);
            builder.AddAttribute(5, "Overlay", true);
            builder.AddAttribute(6, "Variant", DrawerVariant.Temporary);
            builder.AddAttribute(7, "Class", "execution-details-drawer");
            builder.AddAttribute(8, "ChildContent", (RenderFragment)(child =>
            {
                child.OpenComponent<MudText>(0);
                child.AddAttribute(1, "Typo", Typo.h6);
                child.AddAttribute(2, "ChildContent", (RenderFragment)(text => text.AddContent(0, "Execution Details")));
                child.CloseComponent();
                child.OpenComponent<ActivityExecutionDetails>(3);
                child.AddAttribute(4, "ActivityExecution", ActivityExecution);
                child.CloseComponent();
            }));
            builder.CloseComponent();
        }
    }

    private sealed class ActivityExecutionServiceStub : IActivityExecutionService
    {
        public Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, System.Text.Json.Nodes.JsonObject containerActivity, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<ActivityExecutionRecord>());

        public Task<IEnumerable<ActivityExecutionRecordSummary>> ListSummariesAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Enumerable.Empty<ActivityExecutionRecordSummary>());

        public Task<ActivityExecutionRecord?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            Task.FromResult<ActivityExecutionRecord?>(null);

        public Task<ActivityExecutionCallStack> GetCallStackAsync(string activityExecutionId, bool? includeCrossWorkflowChain = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PagedListResponse<Elsa.Api.Client.Resources.Resilience.Models.RetryAttemptRecord>> GetRetriesAsync(string activityInstanceId, int? skip = null, int? take = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PagedListResponse<Elsa.Api.Client.Resources.Resilience.Models.RetryAttemptRecord>());
    }

    private sealed class ClipboardStub : IClipboard
    {
        public Task CopyText(string text, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
