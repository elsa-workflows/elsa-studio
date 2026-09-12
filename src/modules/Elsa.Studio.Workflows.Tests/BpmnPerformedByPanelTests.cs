using System.Text.Json;
using System.Text.Json.Nodes;
using Bunit;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.Scripting.Models;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.ActivityProperties.Tabs;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Tests.Support;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using MudBlazor;
using Xunit;
using static Elsa.Studio.Workflows.Tests.Support.BpmnDocumentFixtures;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers the "Performed by" section: which elements it lets a user bind (an authored task) and which it only describes
/// (an element the document binds automatically, or one that performs no work), that picking an activity and editing
/// its inputs write the task's binding into the document and nothing else, and what it tells the user when the document
/// cannot be read or a save is refused — each refusal with the one next step that applies to it.
/// </summary>
public sealed class BpmnPerformedByPanelTests : BunitContext, IAsyncLifetime
{
    private static readonly BpmnElementSelection TaskSelection = new(TaskId, "serviceTask", "task", "Notify Warehouse", BoundActivityId, "bound", ProcessId, ProcessId, null, null, BpmnBindingKinds.UnboundTask);

    private readonly FakeBpmnDocumentService _documentService = new();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;
    private int _documentChangedCount;
    private int _reloadCount;

    public BpmnPerformedByPanelTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddBpmnEditing(_documentService);
        ComponentFactories.Add<InputsTab, InputsTabStandIn>();
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
        _documentService.ReturnsDocument(Document(), "\"REVISION-1\"");
        Session = new(_documentService, DefinitionId);
    }

    private BpmnDocumentSession Session { get; set; }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void AnAuthoredTask_OffersThePicker_AndItsBoundActivitysOwnInputEditors()
    {
        var cut = RenderPanel(TaskSelection);

        Assert.Contains("Write Line (Elsa.WriteLine)", cut.Find("[data-testid='bpmn-bound-activity']").TextContent);
        Assert.Equal("Change activity", cut.Find("[data-testid='bpmn-pick-activity']").TextContent.Trim());
        var inputs = cut.FindComponent<InputsTab>().Instance;
        Assert.Equal("Elsa.WriteLine", inputs.ActivityDescriptor!.TypeName);
        Assert.Equal("Notifying the warehouse", inputs.Activity!["text"]!["expression"]!["value"]!.GetValue<string>());
    }

    [Fact]
    public void AnAuthoredTaskTheDocumentLeftUnbound_OffersThePickerToBindIt()
    {
        var document = Document();
        ((JsonArray)BpmnDocumentFixtures.TaskElement(document)["extensions"]!["extensionElements"]!).RemoveAt(1);
        UseDocument(document);

        var cut = RenderPanel(TaskSelection with { ActivityId = null, BindingState = "unbound" });

        Assert.Equal("Not bound", cut.Find("[data-testid='bpmn-bound-activity']").TextContent.Trim());
        Assert.Equal("Choose activity", cut.Find("[data-testid='bpmn-pick-activity']").TextContent.Trim());
        Assert.Empty(cut.FindComponents<InputsTab>());
    }

    [Fact]
    public void AnAutomaticallyBoundElement_IsDescribed_ButOffersNothingToEdit()
    {
        var root = RootActivity();
        ((JsonArray)root["activities"]!).Add(new JsonObject { ["id"] = "order-process:node-Timer", ["type"] = "Elsa.Delay", ["version"] = 1 });

        var cut = RenderPanel(new BpmnElementSelection("Timer", "boundaryEvent", "event", "After 1 hour", "order-process:node-Timer", "bound", ProcessId, ProcessId, TaskId, null, BpmnBindingKinds.Automatic), root);

        Assert.Contains("Delay (Elsa.Delay)", cut.Find("[data-testid='bpmn-bound-activity']").TextContent);
        Assert.NotEmpty(cut.FindAll("[data-testid='bpmn-automatic-binding']"));
        Assert.Empty(cut.FindAll("[data-testid='bpmn-pick-activity']"));
        Assert.Empty(cut.FindComponents<InputsTab>());
    }

    [Fact]
    public void AnElementThatPerformsNoWork_SaysSo()
    {
        var cut = RenderPanel(new BpmnElementSelection("StartEvent_1", "startEvent", "event", "Order Received", null, null, ProcessId, ProcessId, null, null));

        Assert.NotEmpty(cut.FindAll("[data-testid='bpmn-no-work']"));
        Assert.Empty(cut.FindAll("[data-testid='bpmn-pick-activity']"));
    }

    [Fact]
    public async Task EditingAnInput_WritesItIntoTheTasksBinding_AndNothingElse()
    {
        var cut = RenderPanel(TaskSelection);
        var inputs = cut.FindComponent<InputsTab>().Instance;

        inputs.Activity!.SetProperty(new WrappedInput { TypeName = "String", Expression = new Expression("JavaScript", "getMessage()") }.SerializeToNode(new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }), "text");
        await cut.InvokeAsync(() => inputs.OnActivityUpdated!(inputs.Activity!));

        var binding = BpmnActivityBindingFormat.Read(BpmnActivityBindingFormat.Find(Session.FindElement(TaskId)!)!);
        Assert.Equal("getMessage()", Assert.Single(binding.Inputs).Value!["expression"]!["value"]!.GetValue<string>());
        Assert.Equal(1, _documentChangedCount);
        Assert.NotEmpty(cut.FindAll("[data-testid='bpmn-unsaved']"));

        // Everything but that one binding is exactly what the server sent.
        var expected = Document();
        BpmnActivityBindingFormat.Attach(BpmnDocumentFixtures.TaskElement(expected), BpmnActivityBindingFormat.Create("Elsa.WriteLine", binding.Inputs.Select(input => KeyValuePair.Create(input.Name, input.Value))));
        Assert.True(JsonNode.DeepEquals(expected, Session.Document));
    }

    [Fact]
    public async Task PickingAnActivity_RebindsTheTask_AndShowsThatActivitysInputs()
    {
        var cut = RenderPanel(TaskSelection);

        await PickAsync(cut, "Elsa.HttpRequest");

        Assert.Equal("Elsa.HttpRequest", BpmnActivityBindingFormat.Read(BpmnActivityBindingFormat.Find(Session.FindElement(TaskId)!)!).ActivityType);
        cut.WaitForAssertion(() => Assert.Equal("Elsa.HttpRequest", cut.FindComponent<InputsTab>().Instance.ActivityDescriptor!.TypeName));
        Assert.Equal(1, _documentChangedCount);
    }

    [Fact]
    public async Task ThePicker_OffersLeafWorkOnly()
    {
        var cut = RenderPanel(TaskSelection);

        await cut.InvokeAsync(() => cut.Find("[data-testid='bpmn-pick-activity']").Click());
        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));

        var offered = _dialogProvider.FindAll("[data-activity-type]").Select(option => option.GetAttribute("data-activity-type")).ToHashSet();
        Assert.Contains("Elsa.HttpRequest", offered);
        Assert.Contains("Elsa.Delay", offered);
        Assert.DoesNotContain("Elsa.Sequence", offered);
        Assert.DoesNotContain("Elsa.If", offered);
        Assert.Contains("PERFORMED BY", _dialogProvider.Markup);
    }

    [Fact]
    public async Task CancellingThePicker_LeavesTheDocumentAsItWas()
    {
        var cut = RenderPanel(TaskSelection);

        await cut.InvokeAsync(() => cut.Find("[data-testid='bpmn-pick-activity']").Click());
        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        _dialogProvider.FindAll("button").Single(button => button.TextContent.Trim() == "Cancel").Click();

        cut.WaitForAssertion(() => Assert.Empty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        Assert.False(Session.IsDirty);
        Assert.Equal(0, _documentChangedCount);
    }

    [Fact]
    public void AStaleDocument_SaysTheWorkflowNeedsReImporting_AndOffersNothingToEdit()
    {
        UseService(new FakeBpmnDocumentService().RefusesGet(BpmnDocumentFailureReason.SourceStale));

        var cut = RenderPanel(TaskSelection);

        Assert.Contains("Import the BPMN file into this workflow again", cut.Find("[data-testid='bpmn-document-load-failure']").TextContent);
        Assert.Empty(cut.FindAll("[data-testid='bpmn-pick-activity']"));
        Assert.Empty(cut.FindComponents<InputsTab>());
    }

    [Fact]
    public async Task ASaveRefusedBecauseTheDefinitionMovedOn_SaysSo_AndOffersReloadRatherThanRetrying()
    {
        _documentService.RefusesPut(BpmnDocumentFailureReason.PreconditionFailed);
        var cut = RenderPanel(TaskSelection);

        await EditAndSaveAsync(cut);

        Assert.Contains("was changed since its BPMN document was read", cut.Find("[data-testid='bpmn-document-save-failure']").TextContent);
        await cut.InvokeAsync(() => cut.Find("[data-testid='bpmn-reload']").Click());
        Assert.Equal(1, _reloadCount);
        Assert.Single(_documentService.Puts);
    }

    [Fact]
    public async Task ASaveRefusedForWantOfAnETag_OffersReload()
    {
        _documentService.RefusesPut(BpmnDocumentFailureReason.PreconditionRequired);
        var cut = RenderPanel(TaskSelection);

        await EditAndSaveAsync(cut);

        Assert.Contains("did not name the revision it replaces", cut.Find("[data-testid='bpmn-document-save-failure']").TextContent);
        Assert.NotEmpty(cut.FindAll("[data-testid='bpmn-reload']"));
    }

    [Fact]
    public async Task ARefusedBinding_ShowsTheServersMessageAgainstTheEditedTask()
    {
        const string message = "The 'Elsa.WriteLine' binding declares an input 'txt', which 'Elsa.WriteLine' does not have.";
        _documentService.RefusesPut(BpmnDocumentFailureReason.BindingInvalid, message);
        var cut = RenderPanel(TaskSelection);

        await EditAndSaveAsync(cut);

        var alert = cut.Find("[data-testid='bpmn-document-save-failure']").TextContent;
        Assert.Contains($"'{TaskId}'", alert);
        Assert.Contains(message, alert);
        Assert.Empty(cut.FindAll("[data-testid='bpmn-reload']"));
    }

    [Fact]
    public void ADocumentWithASubprocess_SaysWhy_AndOffersNoEditItCouldNotSave()
    {
        var document = Document();
        ((JsonArray)document["processes"]![0]!["elements"]!).Add(new JsonObject { ["elementId"] = "Fulfil", ["elementType"] = "subProcess" });
        UseDocument(document);

        var cut = RenderPanel(TaskSelection);

        Assert.Contains("Fulfil", cut.Find("[data-testid='bpmn-document-subprocesses']").TextContent);
        Assert.Contains("Write Line (Elsa.WriteLine)", cut.Find("[data-testid='bpmn-bound-activity']").TextContent);
        Assert.Empty(cut.FindAll("[data-testid='bpmn-pick-activity']"));
        Assert.Empty(cut.FindComponents<InputsTab>());
    }

    [Fact]
    public void ATaskOutsideTheDocument_SaysItCannotBeEditedHere()
    {
        var cut = RenderPanel(TaskSelection with { ElementId = "InsideASubprocess" });

        Assert.NotEmpty(cut.FindAll("[data-testid='bpmn-element-not-in-document']"));
        Assert.Empty(cut.FindAll("[data-testid='bpmn-pick-activity']"));
    }

    private IRenderedComponent<BpmnPerformedByPanel> RenderPanel(BpmnElementSelection selection, JsonObject? rootActivity = null) =>
        Render<BpmnPerformedByPanel>(parameters => parameters
            .Add(x => x.Selection, selection)
            .Add(x => x.RootActivity, rootActivity ?? RootActivity())
            .Add(x => x.Session, Session)
            .Add(x => x.DocumentChanged, () =>
            {
                _documentChangedCount++;
                return Task.CompletedTask;
            })
            .Add(x => x.SaveRequested, () => Session.SaveAsync())
            .Add(x => x.ReloadRequested, () =>
            {
                _reloadCount++;
                return Task.CompletedTask;
            }));

    private void UseDocument(JsonObject document) => UseService(new FakeBpmnDocumentService().ReturnsDocument(document, "\"REVISION-1\""));

    private void UseService(FakeBpmnDocumentService service) => Session = new(service, DefinitionId);

    private async Task PickAsync(IRenderedComponent<BpmnPerformedByPanel> cut, string activityType)
    {
        await cut.InvokeAsync(() => cut.Find("[data-testid='bpmn-pick-activity']").Click());
        _dialogProvider.WaitForAssertion(() => Assert.NotEmpty(_dialogProvider.FindAll("[data-testid='state-machine-activity-option']")));
        _dialogProvider.FindAll("button").Single(button => button.TextContent.Contains("All activities", StringComparison.Ordinal)).Click();
        _dialogProvider.Find($"[data-testid='state-machine-activity-option'][data-activity-type='{activityType}']").Click();
        _dialogProvider.Find("[data-testid='state-machine-activity-picker-commit']").Click();
        cut.WaitForAssertion(() => Assert.True(Session.IsDirty));
    }

    private static async Task EditAsync(IRenderedComponent<BpmnPerformedByPanel> cut)
    {
        var inputs = cut.FindComponent<InputsTab>().Instance;
        inputs.Activity!["text"] = JsonNode.Parse("""{"typeName":"String","expression":{"type":"Literal","value":"edited"}}""");
        await cut.InvokeAsync(() => inputs.OnActivityUpdated!(inputs.Activity!));
    }

    private static async Task EditAndSaveAsync(IRenderedComponent<BpmnPerformedByPanel> cut)
    {
        await EditAsync(cut);
        await cut.InvokeAsync(() => cut.Find("[data-testid='bpmn-save']").Click());
    }

    /// <summary>
    /// Stands in for the real input editors, which need a browser: it keeps the parameters the section hands it, so a
    /// test can edit the activity exactly as an editor would and raise <see cref="InputsTab.OnActivityUpdated"/>.
    /// </summary>
    private sealed class InputsTabStandIn : InputsTab
    {
        protected override Task OnParametersSetAsync() => Task.CompletedTask;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }
}
