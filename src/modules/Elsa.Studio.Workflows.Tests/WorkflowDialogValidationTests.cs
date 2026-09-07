using Bunit;
using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Components.Inputs;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Studio.Workflows.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using MudExtensions.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers the FluentValidation wiring of the workflow dialogs.
/// <para>
/// The create and clone dialogs validate the workflow name with an asynchronous uniqueness rule. Blazor
/// decides whether to raise <c>OnValidSubmit</c> from the synchronous part of <c>EditContext.Validate()</c>,
/// so both dialogs route their form through <c>OnSubmit</c> and await the validation before acting on it.
/// Every test drives the native form submission as well as the dialog's Ok button, because only the button
/// path ever awaited the asynchronous rule.
/// </para>
/// <para>
/// The input dialog builds its validator only after an awaited service call, so its validator component is
/// first created without one and only replaced when the dialog swaps in its final EditContext and EditForm
/// rebuilds its subtree. The stubs below deliberately yield to reproduce that window.
/// </para>
/// </summary>
public sealed class WorkflowDialogValidationTests : BunitContext, IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private const string WorkflowName = "New workflow";
    private const string DuplicateNameMessage = "A workflow with this name already exists.";

    private readonly ControlledWorkflowDefinitionService _workflowDefinitionService = new();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public WorkflowDialogValidationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IWorkflowDefinitionService>(_workflowDefinitionService);
        Services.AddSingleton<IWorkflowRootActivityTemplateProvider>(new DefaultWorkflowRootActivityTemplateProvider());
        Services.AddMudExtensions();
        Services.AddSingleton<IStorageDriverService, DeferredStorageDriverService>();
        Services.AddSingleton<IVariableTypeService, DeferredVariableTypeService>();
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    /// <summary>Identifies how the dialog is submitted, since both routes must obey the same validation.</summary>
    public enum SubmitPath
    {
        /// <summary>A native form submission, which is what pressing Enter in the name field produces.</summary>
        Form,

        /// <summary>The dialog's Ok button, which lives outside the form and calls the component directly.</summary>
        OkButton
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task CreateDialogDoesNotCreateAWorkflowWhenTheNameIsNotUnique(SubmitPath submitPath)
    {
        var dialog = await ShowWorkflowDialogAsync<CreateWorkflowDialog>();
        var validation = _workflowDefinitionService.EnqueueValidation();

        var submitTask = Submit(submitPath);
        await validation.Started.Task.WaitAsync(Timeout);

        Assert.Equal(0, _workflowDefinitionService.CreateCallCount);
        Assert.False(dialog.Result.IsCompleted);

        validation.Result.SetResult(false);
        await submitTask;

        Assert.Equal(0, _workflowDefinitionService.CreateCallCount);
        Assert.False(dialog.Result.IsCompleted);
        _dialogProvider.WaitForAssertion(() => Assert.Contains(DuplicateNameMessage, _dialogProvider.Markup));
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task CreateDialogCreatesTheWorkflowWhenTheNameIsUnique(SubmitPath submitPath)
    {
        var dialog = await ShowWorkflowDialogAsync<CreateWorkflowDialog>();
        var validation = _workflowDefinitionService.EnqueueValidation();

        var submitTask = Submit(submitPath);
        await validation.Started.Task.WaitAsync(Timeout);
        validation.Result.SetResult(true);
        await submitTask;

        var result = await dialog.Result.WaitAsync(Timeout);

        Assert.Equal(WorkflowName, validation.Name);
        Assert.Equal(1, _workflowDefinitionService.CreateCallCount);
        Assert.Equal(WorkflowName, _workflowDefinitionService.CreatedName);
        Assert.False(result?.Canceled);
        Assert.Equal(WorkflowName, Assert.IsType<Result<WorkflowDefinition, ValidationErrors>>(result?.Data).Success?.Name);
        Assert.DoesNotContain(DuplicateNameMessage, _dialogProvider.Markup);
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task CloneDialogDoesNotCloseWhenTheNameIsNotUnique(SubmitPath submitPath)
    {
        var dialog = await ShowWorkflowDialogAsync<CloneWorkflowDialog>();
        var validation = _workflowDefinitionService.EnqueueValidation();

        var submitTask = Submit(submitPath);
        await validation.Started.Task.WaitAsync(Timeout);

        Assert.False(dialog.Result.IsCompleted);

        validation.Result.SetResult(false);
        await submitTask;

        Assert.False(dialog.Result.IsCompleted);
        _dialogProvider.WaitForAssertion(() => Assert.Contains(DuplicateNameMessage, _dialogProvider.Markup));
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task CloneDialogReturnsTheMetadataWhenTheNameIsUnique(SubmitPath submitPath)
    {
        var dialog = await ShowWorkflowDialogAsync<CloneWorkflowDialog>();
        var validation = _workflowDefinitionService.EnqueueValidation();

        var submitTask = Submit(submitPath);
        await validation.Started.Task.WaitAsync(Timeout);
        validation.Result.SetResult(true);
        await submitTask;

        var result = await dialog.Result.WaitAsync(Timeout);

        Assert.Equal(WorkflowName, validation.Name);
        Assert.False(result?.Canceled);
        Assert.Equal(WorkflowName, Assert.IsType<WorkflowMetadataModel>(result?.Data).Name);
        Assert.DoesNotContain(DuplicateNameMessage, _dialogProvider.Markup);
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task InputDialogDoesNotCloseWhenTheNameIsEmpty(SubmitPath submitPath)
    {
        var dialog = await ShowInputDialogAsync();
        await SetInputNameAsync(string.Empty);

        await Submit(submitPath);

        Assert.False(dialog.Result.IsCompleted);
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Please enter a name for the input.", _dialogProvider.Markup));
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task InputDialogClosesWithTheInputWhenTheNameIsValid(SubmitPath submitPath)
    {
        var dialog = await ShowInputDialogAsync();

        await Submit(submitPath);

        var result = await dialog.Result.WaitAsync(Timeout);

        Assert.False(result?.Canceled);
        Assert.Equal("Input1", Assert.IsType<InputDefinition>(result?.Data).Name);
        Assert.DoesNotContain("Please enter a name for the input.", _dialogProvider.Markup);
    }

    private async Task<IDialogReference> ShowDialogAsync<TDialog>(DialogParameters parameters) where TDialog : ComponentBase
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var dialog = await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<TDialog>("Workflow", parameters));
        _dialogProvider.WaitForElement("form");
        return dialog;
    }

    private Task<IDialogReference> ShowWorkflowDialogAsync<TDialog>() where TDialog : ComponentBase =>
        ShowDialogAsync<TDialog>(new() { { nameof(CloneWorkflowDialog.WorkflowName), WorkflowName } });

    // The find and the trigger run on the renderer's dispatcher so that a render cannot invalidate the
    // element's event handler in between. The returned task still completes only when the handler does.
    private Task Submit(SubmitPath submitPath) => _dialogProvider.InvokeAsync(() => submitPath switch
    {
        SubmitPath.Form => _dialogProvider.Find("form").SubmitAsync(),
        SubmitPath.OkButton => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Ok").ClickAsync(new MouseEventArgs()),
        _ => throw new ArgumentOutOfRangeException(nameof(submitPath), submitPath, null)
    });

    private async Task<IDialogReference> ShowInputDialogAsync()
    {
        var dialog = await ShowDialogAsync<EditInputDialog>(
            new DialogParameters<EditInputDialog> { { x => x.WorkflowDefinition, new WorkflowDefinition() } });
        _dialogProvider.WaitForAssertion(() => Assert.Equal("Input1", NameField().Find("input").GetAttribute("value")));
        return dialog;
    }

    private Task SetInputNameAsync(string name) => NameField().Find("input").ChangeAsync(new ChangeEventArgs { Value = name });

    private IRenderedComponent<MudTextField<string>> NameField() => _dialogProvider.FindComponents<MudTextField<string>>()[0];

    /// <summary>
    /// Yields before returning so that the input dialog renders once while its validator is still null.
    /// The dialog then assigns both a new EditContext and the validator, and EditForm rebuilds its subtree
    /// around the new EditContext, which is what gives the validator component its validator.
    /// </summary>
    private sealed class DeferredStorageDriverService : IStorageDriverService
    {
        public async Task<IEnumerable<StorageDriverDescriptor>> GetStorageDriversAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return [new("Workflow", "Workflow", 0, false)];
        }
    }

    /// <inheritdoc cref="DeferredStorageDriverService"/>
    private sealed class DeferredVariableTypeService : IVariableTypeService
    {
        public async Task<IEnumerable<VariableTypeDescriptor>> GetVariableTypesAsync(CancellationToken cancellationToken = default)
        {
            await Task.Yield();
            return [new("System.String", "String", "Text", null)];
        }
    }

    private sealed class ControlledWorkflowDefinitionService : IWorkflowDefinitionService
    {
        private readonly Queue<PendingValidation> _pendingValidations = new();

        public int CreateCallCount { get; private set; }
        public string? CreatedName { get; private set; }

        public PendingValidation EnqueueValidation()
        {
            var validation = new PendingValidation();
            lock (_pendingValidations)
                _pendingValidations.Enqueue(validation);
            return validation;
        }

        public async Task<bool> GetIsNameUniqueAsync(string name, string? definitionId = null, CancellationToken cancellationToken = default)
        {
            PendingValidation validation;
            lock (_pendingValidations)
                validation = _pendingValidations.Dequeue();

            validation.Name = name;
            validation.Started.TrySetResult(true);
            return await validation.Result.Task.WaitAsync(cancellationToken);
        }

        public Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description, string? rootActivityTemplateKey, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
            CreatedName = name;
            return Task.FromResult(new Result<WorkflowDefinition, ValidationErrors>(new WorkflowDefinition
            {
                Name = name,
                Description = description
            }));
        }

        public sealed class PendingValidation
        {
            public string? Name { get; set; }
            public TaskCompletionSource<bool> Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public TaskCompletionSource<bool> Result { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        public Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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
        public Task<string> GenerateUniqueNameAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Result<WorkflowDefinition, ValidationErrors>> CreateNewDefinitionAsync(string name, string? description = null, Action<SaveWorkflowDefinitionRequest>? configureRequest = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> ExportDefinitionAsync(string definitionId, VersionOptions? versionOptions = null, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<FileDownload> BulkExportDefinitionsAsync(IEnumerable<string> ids, bool includeConsumingWorkflows = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<UpdateConsumingWorkflowReferencesResponse> UpdateReferencesAsync(string definitionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ExecuteWorkflowResult> ExecuteAsync(string definitionId, ExecuteWorkflowDefinitionRequest? request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
