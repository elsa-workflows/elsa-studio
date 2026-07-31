using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Api.Client.Resources.OutputConverters.Models;
using Elsa.Api.Client.Resources.VariableTypes.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Components;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs.Outputs.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests.OutputConverters;

public sealed class OutputsTabConverterTests : BunitContext, IAsyncLifetime
{
    private readonly OutputConverterServiceStub _converterService = new();
    private readonly JsonObject _activity = new()
    {
        ["result"] = new JsonObject
        {
            ["typeName"] = "Source",
            ["memoryReference"] = new JsonObject { ["id"] = "result" }
        }
    };

    public OutputsTabConverterTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Services.AddSingleton<IVariableTypeService>(new VariableTypeServiceStub());
        Services.AddSingleton<IOutputConverterService>(_converterService);
        Render<MudPopoverProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void LoadsCompatibleConvertersUsingTheDeclaredBindingTypes()
    {
        var cut = RenderOutputsTab();

        Assert.Equal(("Source", "String"), _converterService.Requests.Single());
        cut.WaitForAssertion(() => Assert.Contains(
            cut.FindComponents<MudSelectItem<string>>(),
            item => item.Instance.Value == "sample.to-text"));
    }

    [Fact]
    public async Task SelectingAndClearingAConverterRoundTripsOnlyConverterJson()
    {
        var cut = RenderOutputsTab();
        cut.WaitForState(() => cut.FindComponents<MudSelect<string>>().Count == 1);
        var converter = cut.FindComponents<MudSelect<string>>().Single();

        await cut.InvokeAsync(() => converter.Instance.ValueChanged.InvokeAsync("sample.to-text"));

        var binding = _activity["result"]!.AsObject();
        Assert.Equal("Source", binding["typeName"]!.GetValue<string>());
        Assert.Equal("result", binding["memoryReference"]!["id"]!.GetValue<string>());
        Assert.Equal("sample.to-text", binding["converter"]!["id"]!.GetValue<string>());

        await cut.InvokeAsync(() => converter.Instance.ValueChanged.InvokeAsync(string.Empty));

        Assert.Null(_activity["result"]!["converter"]);
    }

    [Fact]
    public void UnavailableDiscoveryDoesNotDiscardPersistedConverterConfiguration()
    {
        _activity["result"]!.AsObject()["converter"] = new JsonObject
        {
            ["id"] = "unavailable.converter",
            ["settings"] = new JsonObject { ["format"] = "compact" }
        };
        _converterService.Exception = new InvalidOperationException();

        var cut = RenderOutputsTab();

        Assert.DoesNotContain("Converter", cut.Markup);
        Assert.Equal("unavailable.converter", _activity["result"]!["converter"]!["id"]!.GetValue<string>());
        Assert.Equal("compact", _activity["result"]!["converter"]!["settings"]!["format"]!.GetValue<string>());
    }

    [Fact]
    public async Task ChangingDestinationClearsAnIncompatibleConverter()
    {
        _activity["result"]!.AsObject()["converter"] = new JsonObject { ["id"] = "sample.to-text" };
        var cut = RenderOutputsTab(workflowDefinition: new WorkflowDefinition
        {
            Variables =
            [
                new Variable { Id = "result", Name = "Result", TypeName = "String" },
                new Variable { Id = "count", Name = "Count", TypeName = "Integer" }
            ]
        });
        var bindingTarget = cut.FindComponent<MudSelect<BindingTargetOption>>();

        await cut.InvokeAsync(() => bindingTarget.Instance.ValueChanged.InvokeAsync(new BindingTargetOption("Count", "count", "Integer", false)));

        Assert.Null(_activity["result"]!["converter"]);
    }

    [Fact]
    public void ReadOnlyWorkspaceDisablesConverterSelection()
    {
        var cut = RenderOutputsTab(readOnly: true);

        Assert.All(cut.FindComponents<MudSelect<string>>(), select => Assert.True(select.Instance.Disabled));
    }

    private IRenderedComponent<OutputsTab> RenderOutputsTab(WorkflowDefinition? workflowDefinition = null, bool readOnly = false) => Render<OutputsTab>(parameters => parameters
        .AddCascadingValue<IWorkspace>(new WorkspaceStub(readOnly))
        .Add(x => x.WorkflowDefinition, workflowDefinition ?? new WorkflowDefinition
        {
            Variables = [new Variable { Id = "result", Name = "Result", TypeName = "String" }]
        })
        .Add(x => x.Activity, _activity)
        .Add(x => x.ActivityDescriptor, new ActivityDescriptor
        {
            Outputs = [new OutputDescriptor { Name = "Result", TypeName = "Source", DisplayName = "Result" }]
        })
        .Add(x => x.OnActivityUpdated, (Func<JsonObject, Task>)(_ => Task.CompletedTask)));

    private sealed class VariableTypeServiceStub : IVariableTypeService
    {
        public Task<IEnumerable<VariableTypeDescriptor>> GetVariableTypesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<VariableTypeDescriptor>>([new("Source", "Source", "Tests", null)]);
    }

    private sealed class OutputConverterServiceStub : IOutputConverterService
    {
        public ICollection<(string SourceType, string DestinationType)> Requests { get; } = [];
        public Exception? Exception { get; set; }

        public Task<ICollection<OutputConverterDescriptor>> GetOutputConvertersAsync(string sourceType, string destinationType, CancellationToken cancellationToken = default)
        {
            Requests.Add((sourceType, destinationType));
            if (Exception != null)
                throw Exception;

            if (destinationType == "Integer")
                return Task.FromResult<ICollection<OutputConverterDescriptor>>([]);

            return Task.FromResult<ICollection<OutputConverterDescriptor>>(
            [
                new OutputConverterDescriptor
                {
                    Id = "sample.to-text",
                    SourceTypeName = sourceType,
                    ResultTypeName = destinationType,
                    DisplayName = "Convert to text",
                    Description = "Formats the value as text."
                }
            ]);
        }
    }

    private sealed class WorkspaceStub(bool isReadOnly) : IWorkspace
    {
        public bool IsReadOnly { get; } = isReadOnly;
        public bool HasWorkflowEditPermission => !IsReadOnly;
    }

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
