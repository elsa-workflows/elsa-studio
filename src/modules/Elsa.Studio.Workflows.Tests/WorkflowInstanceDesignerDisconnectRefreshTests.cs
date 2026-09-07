using System.Reflection;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;
using Elsa.Api.Client.Resources.Resilience.Models;
using Elsa.Api.Client.Resources.WorkflowInstances.Enums;
using Elsa.Api.Client.Resources.WorkflowInstances.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowInstanceViewer.Components;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Pins that <see cref="WorkflowInstanceDesigner"/>'s periodic activity-state refresh timer stops
/// quietly instead of crashing the process when the Blazor circuit it belongs to disconnects
/// (see https://github.com/elsa-workflows/elsa-studio/issues/743).
/// </summary>
public sealed class WorkflowInstanceDesignerDisconnectRefreshTests : BunitContext, IAsyncLifetime
{
    public WorkflowInstanceDesignerDisconnectRefreshTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddSingleton<ILocalizer>(new TestLocalizer());
        Services.AddSingleton<IActivityRegistry>(new ActivityRegistryStub());
        Services.AddSingleton<IRemoteFeatureProvider>(new RemoteFeatureProviderStub());
        Services.AddSingleton(DispatchProxy.Create<IDiagramDesignerService, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IDomAccessor, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IActivityVisitor, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowInstanceObserverFactory, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowInstanceService, ThrowingProxy>());
        Services.AddSingleton(DispatchProxy.Create<IWorkflowDefinitionService, ThrowingProxy>());
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task RefreshTickAfterDisposalDoesNotThrowOrCallActivityExecutionService()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");

        await ((IAsyncDisposable)cut.Instance).DisposeAsync();

        var exception = await Record.ExceptionAsync(() => InvokeRefreshTimerTickAsync(cut.Instance, "exec-1"));

        Assert.Null(exception);
        Assert.Equal(0, activityExecutionService.ListSummariesCallCount);
    }

    public static IEnumerable<object[]> CircuitGoneExceptions()
    {
        yield return new object[] { new JSDisconnectedException("The circuit has disconnected.") };
        yield return new object[] { new ObjectDisposedException("ActivityExecutionService") };
        yield return new object[] { new OperationCanceledException("The operation was canceled.") };
    }

    [Theory]
    [MemberData(nameof(CircuitGoneExceptions))]
    public async Task RefreshTickStopsPeriodicRefreshWhenCircuitIsGone(Exception circuitGoneException)
    {
        var activityExecutionService = new RecordingActivityExecutionService(circuitGoneException);
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");
        using var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
        SetRefreshTimer(cut.Instance, timer);

        try
        {
            var exception = await Record.ExceptionAsync(() => InvokeRefreshTimerTickAsync(cut.Instance, "exec-1"));

            Assert.Null(exception);
            Assert.Equal(1, activityExecutionService.ListSummariesCallCount);

            // The periodic refresh timer has been stopped and disposed in response to the circuit-gone
            // exception, so the real Timer can no longer produce a subsequent tick.
            Assert.Null(GetRefreshTimer(cut.Instance));
        }
        finally
        {
            await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        }
    }

    [Fact]
    public async Task ElapsedTickAfterDisposalDoesNothing()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);

        await ((IAsyncDisposable)cut.Instance).DisposeAsync();

        var exception = await Record.ExceptionAsync(() => cut.Instance.ElapsedTimerTickAsync());

        Assert.Null(exception);
        Assert.Equal(0, cut.Instance.NotifyStateChangedCallCount);
    }

    [Fact]
    public async Task ElapsedTickBeforeDisposalNotifiesStateChanged()
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);

        await cut.Instance.ElapsedTimerTickAsync();

        Assert.Equal(1, cut.Instance.NotifyStateChangedCallCount);
    }

    [Theory]
    [MemberData(nameof(CircuitGoneExceptions))]
    public async Task ElapsedTickStopsElapsedTimerWhenCircuitIsGone(Exception circuitGoneException)
    {
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);
        cut.Instance.ThrowOnRender = circuitGoneException;
        using var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
        SetElapsedTimer(cut.Instance, timer);

        try
        {
            var exception = await Record.ExceptionAsync(() => cut.Instance.ElapsedTimerTickAsync());

            Assert.Null(exception);

            // The elapsed timer has been stopped and disposed in response to the circuit-gone exception,
            // so the real Timer can no longer produce a subsequent tick.
            Assert.Null(GetElapsedTimer(cut.Instance));
        }
        finally
        {
            await ((IAsyncDisposable)cut.Instance).DisposeAsync();
        }
    }

    [Fact]
    public async Task RefreshTimerTickRearmToleratesConcurrentlyDisposedTimer()
    {
        var runningRecord = new ActivityExecutionRecord
        {
            Id = "exec-1",
            WorkflowInstanceId = "instance-1",
            ActivityId = "activity-1",
            ActivityNodeId = "node-1",
            ActivityType = "Test",
            Status = ActivityStatus.Running
        };
        var summary = new ActivityExecutionRecordSummary
        {
            Id = "exec-1",
            WorkflowInstanceId = "instance-1",
            ActivityId = "activity-1",
            ActivityNodeId = "node-1",
            ActivityType = "Test",
            Status = ActivityStatus.Running
        };
        var activityExecutionService = new RecordingActivityExecutionService(summariesToReturn: [summary], recordToReturn: runningRecord);
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");

        // Simulate the timer being disposed concurrently (e.g. by DisposeAsync racing this tick) right
        // before the tick tries to rearm it.
        var timer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
        SetRefreshTimer(cut.Instance, timer);
        timer.Dispose();

        var exception = await Record.ExceptionAsync(() => InvokeRefreshTimerTickAsync(cut.Instance, "exec-1"));

        Assert.Null(exception);
    }

    /// <summary>
    /// Pins that the refresh timer is detached from <c>_refreshTimer</c> atomically, before the
    /// (potentially slow) <see cref="Timer.DisposeAsync"/> call completes. To exercise that, the
    /// refresh timer's callback is kept running (blocked on <paramref name="release"/> below) while
    /// the first disposal is in flight, so a second, concurrent disposal genuinely overlaps with it
    /// instead of running after the first has already finished.
    /// </summary>
    [Fact]
    public async Task ConcurrentDisposeCallsDetachTimersAtomicallyWithoutThrowing()
    {
        var timeout = TimeSpan.FromSeconds(5);
        var activityExecutionService = new RecordingActivityExecutionService();
        var cut = RenderDesigner(activityExecutionService);
        SetLastActivityExecution(cut.Instance, "node-1");

        using var started = new ManualResetEventSlim(false);
        using var release = new ManualResetEventSlim(false);
        using var refreshTimer = new Timer(_ =>
        {
            started.Set();
            release.Wait(timeout);
        }, null, Timeout.Infinite, Timeout.Infinite);
        using var elapsedTimer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);

        SetRefreshTimer(cut.Instance, refreshTimer);
        SetElapsedTimer(cut.Instance, elapsedTimer);

        // Fire the refresh timer's callback immediately and wait for it to actually start running.
        refreshTimer.Change(TimeSpan.Zero, Timeout.InfiniteTimeSpan);
        Assert.True(started.Wait(timeout), "The refresh timer callback did not start in time.");

        var disposable = (IAsyncDisposable)cut.Instance;

        // System.Threading.Timer.DisposeAsync only completes once any in-flight callback finishes, so
        // this first disposal stays pending while the callback above is blocked on `release`.
        var firstDisposeTask = disposable.DisposeAsync().AsTask();

        // The atomic Interlocked.Exchange detach in StopRefreshActivityStatePeriodically happens
        // before the timer is awaited, so the field is already cleared while the first disposal is
        // still pending. Against the previous check/await/clear implementation, this assertion would
        // still hold, but the second call below would then observe a non-null field and race to
        // dispose/clear it itself instead of being a no-op.
        Assert.Null(GetRefreshTimer(cut.Instance));

        var secondDisposeException = await Record.ExceptionAsync(() => disposable.DisposeAsync().AsTask());

        Assert.Null(secondDisposeException);
        Assert.False(firstDisposeTask.IsCompleted, "The first disposal should still be pending on the blocked callback.");

        release.Set();

        var completedTask = await Task.WhenAny(firstDisposeTask, Task.Delay(timeout));
        Assert.Same(firstDisposeTask, completedTask);

        var firstDisposeException = await Record.ExceptionAsync(() => firstDisposeTask);

        Assert.Null(firstDisposeException);
        Assert.Null(GetRefreshTimer(cut.Instance));
        Assert.Null(GetElapsedTimer(cut.Instance));
    }

    private IRenderedComponent<TestWorkflowInstanceDesigner> RenderDesigner(IActivityExecutionService activityExecutionService)
    {
        Services.AddSingleton(activityExecutionService);

        var workflowInstance = new WorkflowInstance
        {
            Id = "instance-1",
            DefinitionId = "definition-1",
            Status = WorkflowStatus.Finished
        };

        return Render<TestWorkflowInstanceDesigner>(parameters => parameters
            .Add(x => x.WorkflowInstance, workflowInstance));
    }

    private static void SetLastActivityExecution(WorkflowInstanceDesigner instance, string activityNodeId)
    {
        var property = typeof(WorkflowInstanceDesigner).GetProperty("LastActivityExecution", BindingFlags.Instance | BindingFlags.NonPublic)!;
        property.SetValue(instance, new ActivityExecutionRecord
        {
            Id = "exec-1",
            WorkflowInstanceId = "instance-1",
            ActivityId = "activity-1",
            ActivityNodeId = activityNodeId,
            ActivityType = "Test",
            Status = ActivityStatus.Running
        });
    }

    private const string RefreshTimerFieldName = "_refreshTimer";
    private const string ElapsedTimerFieldName = "_elapsedTimer";

    private static void SetRefreshTimer(WorkflowInstanceDesigner instance, Timer timer) =>
        SetTimer(instance, RefreshTimerFieldName, timer);

    private static Timer? GetRefreshTimer(WorkflowInstanceDesigner instance) =>
        GetTimer(instance, RefreshTimerFieldName);

    private static Task InvokeRefreshTimerTickAsync(WorkflowInstanceDesigner instance, string activityExecutionRecordId) =>
        instance.RefreshTimerTickAsync(activityExecutionRecordId);

    private static void SetElapsedTimer(WorkflowInstanceDesigner instance, Timer timer) =>
        SetTimer(instance, ElapsedTimerFieldName, timer);

    private static Timer? GetElapsedTimer(WorkflowInstanceDesigner instance) =>
        GetTimer(instance, ElapsedTimerFieldName);

    private static Timer? GetTimer(WorkflowInstanceDesigner instance, string fieldName) =>
        (Timer?)GetTimerField(fieldName).GetValue(instance);

    private static void SetTimer(WorkflowInstanceDesigner instance, string fieldName, Timer? value) =>
        GetTimerField(fieldName).SetValue(instance, value);

    private static FieldInfo GetTimerField(string fieldName) =>
        typeof(WorkflowInstanceDesigner).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!;

    /// <summary>
    /// A <see cref="WorkflowInstanceDesigner"/> whose state-changed notification can be made to throw
    /// on demand. bUnit's test renderer does not propagate exceptions from the JS-interop-driven
    /// render pipeline back through <c>InvokeAsync(StateHasChanged)</c> the way a real Blazor circuit
    /// does, so the internal <see cref="WorkflowInstanceDesigner.NotifyStateChangedAsync"/> seam is
    /// overridden here to simulate the circuit-gone exception that a real disconnect would surface
    /// from that call.
    /// </summary>
    private sealed class TestWorkflowInstanceDesigner : WorkflowInstanceDesigner
    {
        public Exception? ThrowOnRender { get; set; }
        public int NotifyStateChangedCallCount { get; private set; }

        protected override Task OnAfterRenderAsync(bool firstRender) => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }

        internal override Task NotifyStateChangedAsync()
        {
            NotifyStateChangedCallCount++;
            return ThrowOnRender != null ? Task.FromException(ThrowOnRender) : base.NotifyStateChangedAsync();
        }
    }

    /// <summary>
    /// An <see cref="IActivityExecutionService"/> that counts calls to <see cref="ListSummariesAsync"/>
    /// and, when constructed with an exception, throws it from that call to simulate a circuit
    /// disconnecting mid-refresh.
    /// </summary>
    private sealed class RecordingActivityExecutionService(
        Exception? exceptionToThrow = null,
        IEnumerable<ActivityExecutionRecordSummary>? summariesToReturn = null,
        ActivityExecutionRecord? recordToReturn = null) : IActivityExecutionService
    {
        public int ListSummariesCallCount { get; private set; }

        public Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IEnumerable<ActivityExecutionRecordSummary>> ListSummariesAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default)
        {
            ListSummariesCallCount++;

            if (exceptionToThrow != null)
                throw exceptionToThrow;

            return Task.FromResult(summariesToReturn ?? []);
        }

        public Task<ActivityExecutionRecord?> GetAsync(string id, CancellationToken cancellationToken = default) =>
            recordToReturn != null ? Task.FromResult<ActivityExecutionRecord?>(recordToReturn) : throw new NotSupportedException();

        public Task<ActivityExecutionCallStack> GetCallStackAsync(string activityExecutionId, bool? includeCrossWorkflowChain = null, int? skip = null, int? take = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PagedListResponse<RetryAttemptRecord>> GetRetriesAsync(string activityInstanceId, int? skip = null, int? take = null, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class ActivityRegistryStub : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor> List() => throw new NotSupportedException();
        public Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor? Find(string activityType, int? version = null) => throw new NotSupportedException();
        public IEnumerable<Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor> FindAll(string activityType) => throw new NotSupportedException();
        public void MarkStale() => throw new NotSupportedException();
    }

    private sealed class RemoteFeatureProviderStub : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<IEnumerable<Elsa.Api.Client.Resources.Features.Models.FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    /// <summary>
    /// A <see cref="DispatchProxy"/> that throws for every call, used for services this component
    /// depends on but that these tests never exercise.
    /// </summary>
    private class ThrowingProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"Unexpected call to {targetMethod!.DeclaringType!.Name}.{targetMethod.Name}.");
    }
}
