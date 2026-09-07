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
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Components.Outputs;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Services;
using Elsa.Studio.Workflows.Models;
using Elsa.Studio.Workflows.Tests.Support;
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
/// The input and output dialogs populate their default field values (e.g. "Input1"/"Output1") only after
/// awaiting the storage driver and variable type services, so the stubs below deliberately yield to
/// reproduce that delay and the tests wait for the resulting render before interacting with the form.
/// </para>
/// </summary>
public sealed class WorkflowDialogValidationTests : BunitContext, IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private const string WorkflowName = "New workflow";
    private const string DuplicateNameMessage = "A workflow with this name already exists.";

    private readonly ControlledWorkflowDefinitionService _workflowDefinitionService = new();
    private readonly DeferredStorageDriverService _storageDriverService = new();
    private readonly DeferredVariableTypeService _variableTypeService = new();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public WorkflowDialogValidationTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IWorkflowDefinitionService>(_workflowDefinitionService);
        Services.AddSingleton<IWorkflowRootActivityTemplateProvider>(new DefaultWorkflowRootActivityTemplateProvider());
        Services.AddMudExtensions();
        Services.AddSingleton<IStorageDriverService>(_storageDriverService);
        Services.AddSingleton<IVariableTypeService>(_variableTypeService);
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
    public async Task CreateDialogDoesNotCreateAWorkflowWhenTheNameChangesWhileTheUniquenessCheckIsPending(SubmitPath submitPath)
    {
        const string changedName = "Renamed workflow";
        var dialog = await ShowWorkflowDialogAsync<CreateWorkflowDialog>();
        var submitValidation = _workflowDefinitionService.EnqueueValidation();

        var submitTask = Submit(submitPath);
        await submitValidation.Started.Task.WaitAsync(Timeout);

        // Changing the name while the submission's uniqueness check is still pending triggers Blazilla's
        // own field-changed validation, which runs a second (unrelated) uniqueness check for the new value.
        var fieldChangeValidation = _workflowDefinitionService.EnqueueValidation();
        await SetNameAsync(changedName);
        await fieldChangeValidation.Started.Task.WaitAsync(Timeout);
        fieldChangeValidation.Result.SetResult(true);

        submitValidation.Result.SetResult(true);
        await submitTask;

        Assert.Equal(WorkflowName, submitValidation.Name);
        Assert.Equal(0, _workflowDefinitionService.CreateCallCount);
        Assert.False(dialog.Result.IsCompleted);

        var retryValidation = _workflowDefinitionService.EnqueueValidation();
        var retrySubmitTask = Submit(submitPath);
        await retryValidation.Started.Task.WaitAsync(Timeout);
        retryValidation.Result.SetResult(true);
        await retrySubmitTask;

        var result = await dialog.Result.WaitAsync(Timeout);

        Assert.Equal(changedName, retryValidation.Name);
        Assert.Equal(1, _workflowDefinitionService.CreateCallCount);
        Assert.Equal(changedName, _workflowDefinitionService.CreatedName);
        Assert.False(result?.Canceled);
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
    public async Task CloneDialogDoesNotCloseWhenTheNameChangesWhileTheUniquenessCheckIsPending(SubmitPath submitPath)
    {
        const string changedName = "Renamed clone";
        var dialog = await ShowWorkflowDialogAsync<CloneWorkflowDialog>();
        var submitValidation = _workflowDefinitionService.EnqueueValidation();

        var submitTask = Submit(submitPath);
        await submitValidation.Started.Task.WaitAsync(Timeout);

        // Changing the name while the submission's uniqueness check is still pending triggers Blazilla's
        // own field-changed validation, which runs a second (unrelated) uniqueness check for the new value.
        var fieldChangeValidation = _workflowDefinitionService.EnqueueValidation();
        await SetNameAsync(changedName);
        await fieldChangeValidation.Started.Task.WaitAsync(Timeout);
        fieldChangeValidation.Result.SetResult(true);

        submitValidation.Result.SetResult(true);
        await submitTask;

        Assert.Equal(WorkflowName, submitValidation.Name);
        Assert.False(dialog.Result.IsCompleted);

        var retryValidation = _workflowDefinitionService.EnqueueValidation();
        var retrySubmitTask = Submit(submitPath);
        await retryValidation.Started.Task.WaitAsync(Timeout);
        retryValidation.Result.SetResult(true);
        await retrySubmitTask;

        var result = await dialog.Result.WaitAsync(Timeout);

        Assert.Equal(changedName, retryValidation.Name);
        Assert.False(result?.Canceled);
        Assert.Equal(changedName, Assert.IsType<WorkflowMetadataModel>(result?.Data).Name);
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task InputDialogDoesNotCloseWhenTheNameIsEmpty(SubmitPath submitPath)
    {
        var dialog = await ShowInputDialogAsync();
        await SetNameAsync(string.Empty);

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

    [Fact]
    public async Task InputDialogLoadsDescriptorsOnceAcrossInitialRenderAndParentReRender()
    {
        await ShowInputDialogAsync();

        Assert.Equal(1, _storageDriverService.CallCount);
        Assert.Equal(1, _variableTypeService.CallCount);

        var editInputDialog = _dialogProvider.FindComponent<EditInputDialog>();
        editInputDialog.Render(parameters => parameters
            .Add(x => x.WorkflowDefinition, new WorkflowDefinition()));

        Assert.Equal(1, _storageDriverService.CallCount);
        Assert.Equal(1, _variableTypeService.CallCount);
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task OutputDialogDoesNotCloseWhenTheNameIsEmpty(SubmitPath submitPath)
    {
        var dialog = await ShowOutputDialogAsync();
        await SetNameAsync(string.Empty);

        await Submit(submitPath);

        Assert.False(dialog.Result.IsCompleted);
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Please enter a name for the output.", _dialogProvider.Markup));
    }

    [Theory]
    [InlineData(SubmitPath.Form)]
    [InlineData(SubmitPath.OkButton)]
    public async Task OutputDialogClosesWithTheOutputWhenTheNameIsValid(SubmitPath submitPath)
    {
        var dialog = await ShowOutputDialogAsync();

        await Submit(submitPath);

        var result = await dialog.Result.WaitAsync(Timeout);

        Assert.False(result?.Canceled);
        Assert.Equal("Output1", Assert.IsType<OutputDefinition>(result?.Data).Name);
        Assert.DoesNotContain("Please enter a name for the output.", _dialogProvider.Markup);
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

    private async Task<IDialogReference> ShowOutputDialogAsync()
    {
        var dialog = await ShowDialogAsync<EditOutputDialog>(
            new DialogParameters<EditOutputDialog> { { x => x.WorkflowDefinition, new WorkflowDefinition() } });
        _dialogProvider.WaitForAssertion(() => Assert.Equal("Output1", NameField().Find("input").GetAttribute("value")));
        return dialog;
    }

    private Task SetNameAsync(string name) => NameField().Find("input").ChangeAsync(new ChangeEventArgs { Value = name });

    private IRenderedComponent<MudTextField<string>> NameField() => _dialogProvider.FindComponents<MudTextField<string>>()[0];

    /// <summary>
    /// Yields before returning so that the input and output dialogs render once with their default field
    /// values still unset, reproducing the delay the tests wait out before interacting with the form.
    /// Also counts calls so tests can assert the input dialog loads its descriptors once, in
    /// <c>OnInitializedAsync</c>, rather than on every parent re-render.
    /// </summary>
    private sealed class DeferredStorageDriverService : IStorageDriverService
    {
        public int CallCount { get; private set; }

        public async Task<IEnumerable<StorageDriverDescriptor>> GetStorageDriversAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            await Task.Yield();
            return [new("Workflow", "Workflow", 0, false)];
        }
    }

    /// <inheritdoc cref="DeferredStorageDriverService"/>
    private sealed class DeferredVariableTypeService : IVariableTypeService
    {
        public int CallCount { get; private set; }

        public async Task<IEnumerable<VariableTypeDescriptor>> GetVariableTypesAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            await Task.Yield();
            return [new("System.String", "String", "Text", null)];
        }
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] =>
            new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
