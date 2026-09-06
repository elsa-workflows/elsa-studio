using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.DomInterop.Models;
using Elsa.Studio.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Extensions;
using Elsa.Studio.Workflows.Shared.Components;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

public sealed class WorkflowEditorLifecycleTests : BunitContext, IAsyncLifetime
{
    private readonly DelayedWorkflowDefinitionEditorService _editorService = new();
    private readonly RecordingUserMessageService _userMessageService = new();

    public WorkflowEditorLifecycleTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddLogging();
        Services.AddCoreInternal();
        Services.AddRemoteBackend();
        Services.AddWorkflowsModule();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IWorkflowDefinitionEditorService>(_editorService);
        Services.AddSingleton<IActivityRegistry, NoOpActivityRegistry>();
        Services.AddSingleton<IDomAccessor, NoOpDomAccessor>();
        Services.AddSingleton(_userMessageService);
        Services.AddSingleton<IUserMessageService>(_userMessageService);
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task SaveSuccessCompletedAfterInvalidationDoesNotReplaceOrInvokeSuccess()
    {
        var originalDefinition = CreateDefinition("initial");
        var replacementCount = 0;
        var cut = RenderEditor(originalDefinition, () =>
        {
            replacementCount++;
            return Task.CompletedTask;
        });
        var successCount = 0;

        var saveTask = InvokeSaveAsync(cut.Instance, _ =>
        {
            successCount++;
            return Task.CompletedTask;
        }, null);
        var pendingSave = await _editorService.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.InvalidateWorkflowDefinitionOperations();
        await pendingSave.CompleteSuccessAsync(CreateDefinition("server response"));
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(originalDefinition, cut.Instance.WorkflowDefinition);
        Assert.True(pendingSave.CallbackInvoked);
        Assert.Equal(0, replacementCount);
        Assert.Equal(0, successCount);
    }

    [Fact]
    public async Task SaveFailureCompletedAfterInvalidationDoesNotInvokeFailure()
    {
        var originalDefinition = CreateDefinition("initial");
        var cut = RenderEditor(originalDefinition, () => Task.CompletedTask);
        var failureCount = 0;

        var saveTask = InvokeSaveAsync(cut.Instance, null, _ =>
        {
            failureCount++;
            return Task.CompletedTask;
        });
        var pendingSave = await _editorService.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.InvalidateWorkflowDefinitionOperations();
        pendingSave.CompleteFailure();
        await saveTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, failureCount);
        Assert.Equal(0, _userMessageService.MessageCount);
    }

    [Fact]
    public async Task RetractSuccessCompletedAfterInvalidationDoesNotReplaceOrInvokeSuccess()
    {
        var originalDefinition = CreateDefinition("initial");
        var replacementCount = 0;
        var cut = RenderEditor(originalDefinition, () =>
        {
            replacementCount++;
            return Task.CompletedTask;
        });
        var successCount = 0;

        var retractTask = InvokeRetractAsync(cut.Instance, () =>
        {
            successCount++;
            return Task.CompletedTask;
        }, null);
        var pendingRetract = await _editorService.RetractStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.InvalidateWorkflowDefinitionOperations();
        await pendingRetract.CompleteSuccessAsync(CreateDefinition("retracted response"));
        await retractTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Same(originalDefinition, cut.Instance.WorkflowDefinition);
        Assert.True(pendingRetract.CallbackInvoked);
        Assert.Equal(0, replacementCount);
        Assert.Equal(0, successCount);
    }

    [Fact]
    public async Task RetractFailureCompletedAfterInvalidationDoesNotInvokeFailure()
    {
        var originalDefinition = CreateDefinition("initial");
        var cut = RenderEditor(originalDefinition, () => Task.CompletedTask);
        var failureCount = 0;

        var retractTask = InvokeRetractAsync(cut.Instance, null, _ =>
        {
            failureCount++;
            return Task.CompletedTask;
        });
        var pendingRetract = await _editorService.RetractStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cut.Instance.InvalidateWorkflowDefinitionOperations();
        pendingRetract.CompleteFailure();
        await retractTask.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(0, failureCount);
    }

    private IRenderedComponent<WorkflowEditor> RenderEditor(WorkflowDefinition definition, Func<Task> workflowDefinitionUpdated)
    {
        ComponentFactories.Add<DiagramDesignerWrapper, TestDiagramDesignerWrapper>();
        ComponentFactories.Add<ActivityPropertiesPanel, TestActivityPropertiesPanel>();
        return Render<WorkflowEditor>(parameters => parameters
            .Add(x => x.WorkflowDefinition, definition)
            .Add(x => x.WorkflowDefinitionUpdated, workflowDefinitionUpdated));
    }

    private static Task InvokeSaveAsync(WorkflowEditor editor, Func<SaveWorkflowDefinitionResponse, Task>? onSuccess, Func<ValidationErrors, Task>? onFailure)
    {
        var method = typeof(WorkflowEditor).GetMethod("SaveChangesAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(editor, new object?[] { false, false, false, onSuccess, onFailure, null })!;
    }

    private static Task InvokeRetractAsync(WorkflowEditor editor, Func<Task>? onSuccess, Func<ValidationErrors, Task>? onFailure)
    {
        var method = typeof(WorkflowEditor).GetMethod("RetractAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        return (Task)method.Invoke(editor, new object?[] { onSuccess, onFailure })!;
    }

    private static WorkflowDefinition CreateDefinition(string name) => new()
    {
        Id = "version-1",
        DefinitionId = "definition-1",
        Name = name,
        Description = "description"
    };

    private sealed class DelayedWorkflowDefinitionEditorService : IWorkflowDefinitionEditorService
    {
        public TaskCompletionSource<PendingSave> SaveStarted { get; } = NewCompletionSource<PendingSave>();
        public TaskCompletionSource<PendingRetract> RetractStarted { get; } = NewCompletionSource<PendingRetract>();

        public Task<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> SaveAsync(WorkflowDefinition workflowDefinition, bool publish, Func<WorkflowDefinition, Task>? workflowSavedCallback = null, CancellationToken cancellationToken = default)
        {
            var pending = new PendingSave(workflowSavedCallback);
            SaveStarted.TrySetResult(pending);
            return pending.Completion.Task;
        }

        public Task<SaveWorkflowDefinitionResponse> PublishAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowPublishedCallback = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<Result<WorkflowDefinition, ValidationErrors>> RetractAsync(WorkflowDefinition workflowDefinition, Func<WorkflowDefinition, Task>? workflowRetractedCallback = null, CancellationToken cancellationToken = default)
        {
            var pending = new PendingRetract(workflowRetractedCallback);
            RetractStarted.TrySetResult(pending);
            return pending.Completion.Task;
        }

        public Task<FileDownload> ExportAsync(WorkflowDefinition workflowDefinition, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class PendingSave(Func<WorkflowDefinition, Task>? callback)
    {
        public TaskCompletionSource<Result<SaveWorkflowDefinitionResponse, ValidationErrors>> Completion { get; } = NewCompletionSource<Result<SaveWorkflowDefinitionResponse, ValidationErrors>>();
        public bool CallbackInvoked { get; private set; }

        public async Task CompleteSuccessAsync(WorkflowDefinition definition)
        {
            if (callback != null)
            {
                CallbackInvoked = true;
                await callback(definition);
            }

            Completion.TrySetResult(new(new SaveWorkflowDefinitionResponse(definition, false, 0)));
        }

        public void CompleteFailure() => Completion.TrySetResult(new(new ValidationErrors([new("save failed")])));
    }

    private sealed class PendingRetract(Func<WorkflowDefinition, Task>? callback)
    {
        public TaskCompletionSource<Result<WorkflowDefinition, ValidationErrors>> Completion { get; } = NewCompletionSource<Result<WorkflowDefinition, ValidationErrors>>();
        public bool CallbackInvoked { get; private set; }

        public async Task CompleteSuccessAsync(WorkflowDefinition definition)
        {
            if (callback != null)
            {
                CallbackInvoked = true;
                await callback(definition);
            }

            Completion.TrySetResult(new(definition));
        }

        public void CompleteFailure() => Completion.TrySetResult(new(new ValidationErrors([new("retract failed")])));
    }

    private sealed class NoOpActivityRegistry : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => [];
        public ActivityDescriptor? Find(string activityType, int? version = null) => null;
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => [];
        public void MarkStale()
        {
        }
    }

    private sealed class NoOpDomAccessor : IDomAccessor
    {
        public Task<DomRect> GetBoundingClientRectAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.FromResult(new DomRect());
        public Task<double> GetVisibleHeightAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.FromResult(0d);
        public Task ClickElementAsync(ElementRef elementRef, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class RecordingUserMessageService : IUserMessageService
    {
        public int MessageCount { get; private set; }
        public void ShowSnackbarTextMessage(string message, Severity severity = Severity.Normal, Action<SnackbarOptions>? snackbarOptions = null) => MessageCount++;
        public void ShowSnackbarTextMessage(IEnumerable<string> messages, Severity severity = Severity.Normal, Action<SnackbarOptions>? snackbarOptions = null) => MessageCount++;
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }

    private sealed class TestDiagramDesignerWrapper : DiagramDesignerWrapper
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private sealed class TestActivityPropertiesPanel : ActivityPropertiesPanel
    {
        protected override Task OnInitializedAsync() => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }

    private static TaskCompletionSource<T> NewCompletionSource<T>() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
