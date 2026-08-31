using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.DiagramDesigners.StateMachines.Presentation;
using Elsa.Studio.Workflows.Domain.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.StateMachines;

public sealed class StateMachineActivityPickerDialogTests : BunitContext, IAsyncLifetime
{
    private readonly ActivityDescriptor _writeLine = new()
    {
        Name = "WriteLine",
        DisplayName = "Write line",
        TypeName = "Elsa.WriteLine",
        Category = "Primitives",
        Description = "Writes text to the log.",
        IsBrowsable = true
    };

    private readonly ActivityDescriptor _http = new()
    {
        Name = "HttpEndpoint",
        DisplayName = "HTTP endpoint",
        TypeName = "Elsa.HttpEndpoint",
        Category = "HTTP",
        Description = "Waits for an incoming HTTP request.",
        IsBrowsable = true
    };

    private readonly ActivityDescriptor _sequence = new()
    {
        Name = "Sequence",
        DisplayName = "Sequence",
        TypeName = "Elsa.Sequence",
        Category = "Composition",
        Description = "Runs a sequence of activities.",
        IsBrowsable = true
    };

    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public StateMachineActivityPickerDialogTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IActivityRegistry>(new ActivityRegistryStub([_writeLine, _http, _sequence]));
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task Picker_LoadsBrowsableActivitiesAndFiltersEverySearchField()
    {
        var dialog = await ShowDialogAsync();

        _dialogProvider.WaitForAssertion(() => Assert.Equal(3, _dialogProvider.FindAll("[data-activity-type]").Count));
        var search = _dialogProvider.Find("input[id*='state-machine-activity-picker']");

        search.Input("incoming");
        Assert.Single(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']"));
        Assert.Contains("HTTP endpoint", _dialogProvider.Find("[data-testid='state-machine-activity-option']").TextContent);

        search.Input("Elsa.WriteLine");
        Assert.Single(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']"));
        Assert.Contains("Write line", _dialogProvider.Find("[data-testid='state-machine-activity-option']").TextContent);

        search.Input("Primitives");
        Assert.Single(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']"));

        search.Input("missing activity");
        Assert.Contains("No matching activities", _dialogProvider.Markup);

        _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Cancel").Click();
        Assert.True((await dialog.Result)?.Canceled);
    }

    [Fact]
    public async Task Picker_ReturnsDescriptorOnlyAfterExplicitSelection()
    {
        var dialog = await ShowDialogAsync();

        Assert.False(dialog.Result.IsCompleted);
        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        _dialogProvider.Find("[data-testid='state-machine-activity-option'][data-activity-type='Elsa.WriteLine']").Click();

        var result = await dialog.Result;
        Assert.False(result?.Canceled);
        Assert.Same(_writeLine, result?.Data);
    }

    [Fact]
    public async Task Picker_OffersSequenceAsTheProminentMultiActivityShortcut()
    {
        var dialog = await ShowDialogAsync();

        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-picker-sequence']")));
        _dialogProvider.Find("[data-testid='state-machine-activity-picker-sequence']").Click();

        var result = await dialog.Result;
        Assert.Same(_sequence, result?.Data);
    }

    private async Task<IDialogReference> ShowDialogAsync()
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        return await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<StateMachineActivityPickerDialog>("Add action"));
    }

    private sealed class ActivityRegistryStub(IEnumerable<ActivityDescriptor> activities) : IActivityRegistry
    {
        public Task RefreshAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task EnsureLoadedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public IEnumerable<ActivityDescriptor> List() => activities;
        public ActivityDescriptor? Find(string activityType, int? version = null) => activities.FirstOrDefault(x => x.TypeName == activityType);
        public IEnumerable<ActivityDescriptor> FindAll(string activityType) => activities.Where(x => x.TypeName == activityType);
        public void MarkStale()
        {
        }
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
