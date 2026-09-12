using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.DiagramDesigners.Bpmn;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Tests.Support;
using Xunit;
using static Elsa.Studio.Workflows.Tests.Support.BpmnDocumentFixtures;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnDocumentSession"/>: a binding edit is written back only through the document PUT, carrying the
/// ETag of the revision it was made in; a refusal is reported and never retried; and a document whose subprocess bodies
/// the PUT would empty is refused before anything is sent — in both directions, since a save that silently went through
/// looks exactly like one that worked.
/// </summary>
public class BpmnDocumentSessionTests
{
    private const string ETag = "\"REVISION-1\"";
    private readonly FakeBpmnDocumentService _service = new();
    private readonly BpmnDocumentSession _session;

    public BpmnDocumentSessionTests()
    {
        _service.ReturnsDocument(Document(), ETag);
        _session = new(_service, DefinitionId);
    }

    [Fact]
    public async Task SaveAsync_PutsTheEditedDocumentWithTheETagItWasReadAt_ThenReadsTheServersViewBack()
    {
        await _session.EnsureLoadedAsync();
        _service.AcceptsPut("\"REVISION-2\"").ReturnsDocument(Document(), "\"REVISION-2\"");
        var binding = BindHttpRequest();

        var result = await _session.SaveAsync();

        Assert.True(result.IsSuccess);
        var put = Assert.Single(_service.Puts);
        Assert.Equal((DefinitionId, ETag), (put.DefinitionId, put.IfMatch));
        Assert.True(JsonNode.DeepEquals(binding, BpmnActivityBindingFormat.Find(BpmnDocumentFixtures.TaskElement(put.Document))));
        Assert.Equal(2, _service.GetCount);
        Assert.False(_session.IsDirty);
        Assert.Empty(_session.EditedElementIds);
    }

    [Fact]
    public async Task SaveAsync_WhenTheRevisionMovedOn_ReportsTheConflict_KeepsTheEditAndDoesNotRetry()
    {
        await _session.EnsureLoadedAsync();
        _service.RefusesPut(BpmnDocumentFailureReason.PreconditionFailed);
        BindHttpRequest();

        var result = await _session.SaveAsync();

        Assert.Equal(BpmnDocumentFailureReason.PreconditionFailed, result.Failure!.Reason);
        Assert.Equal(BpmnDocumentFailureReason.PreconditionFailed, _session.SaveFailure!.Reason);
        Assert.Single(_service.Puts);
        Assert.Equal(1, _service.GetCount);
        Assert.True(_session.IsDirty);
        Assert.Equal([TaskId], _session.EditedElementIds);
    }

    [Fact]
    public async Task SaveAsync_RefusesADocumentWithASubprocess_WithoutSendingIt()
    {
        var document = Document();
        ((JsonArray)document["processes"]![0]!["elements"]!).Add(new JsonObject { ["elementId"] = "Fulfil", ["elementType"] = "subProcess" });
        var service = new FakeBpmnDocumentService().ReturnsDocument(document, ETag).AcceptsPut("\"REVISION-2\"");
        var session = new BpmnDocumentSession(service, DefinitionId);
        await session.EnsureLoadedAsync();
        session.SetBinding(TaskId, BpmnActivityBindingFormat.Create("Elsa.HttpRequest", []));

        var result = await session.SaveAsync();

        Assert.Equal(BpmnDocumentFailureReason.SubProcessContentNotCarried, result.Failure!.Reason);
        Assert.Empty(service.Puts);
        Assert.Equal(["Fulfil"], session.SubProcessIds);
    }

    [Fact]
    public async Task SaveAsync_SendsADocumentWithoutSubprocesses()
    {
        await _session.EnsureLoadedAsync();
        _service.AcceptsPut("\"REVISION-2\"");
        BindHttpRequest();

        await _session.SaveAsync();

        Assert.Single(_service.Puts);
    }

    [Fact]
    public async Task Discard_RestoresTheRevisionTheServerReturned()
    {
        await _session.EnsureLoadedAsync();
        BindHttpRequest();
        Assert.True(_session.IsDirty);

        _session.Discard();

        Assert.False(_session.IsDirty);
        Assert.True(JsonNode.DeepEquals(Document(), _session.Document));
        Assert.Empty(_session.EditedElementIds);
    }

    [Fact]
    public async Task EnsureLoadedAsync_ReportsAStaleDocument_AndLeavesNothingToEdit()
    {
        var service = new FakeBpmnDocumentService().RefusesGet(BpmnDocumentFailureReason.SourceStale);
        var session = new BpmnDocumentSession(service, DefinitionId);

        await session.EnsureLoadedAsync();

        Assert.Equal(BpmnDocumentFailureReason.SourceStale, session.LoadFailure!.Reason);
        Assert.Null(session.Document);
        Assert.Equal(BpmnDocumentFailureReason.Unknown, (await session.SaveAsync()).Failure!.Reason);
        Assert.Empty(service.Puts);
    }

    [Fact]
    public async Task EnsureLoadedAsync_ReportsAnUnreachableServer_InsteadOfThrowing()
    {
        var service = new FakeBpmnDocumentService { GetException = new HttpRequestException("connection refused") };
        var session = new BpmnDocumentSession(service, DefinitionId);

        await session.EnsureLoadedAsync();

        Assert.Equal("connection refused", session.LoadFailure!.Message);
    }

    [Fact]
    public async Task EnsureLoadedAsync_ReadsOnce()
    {
        await _session.EnsureLoadedAsync();
        await _session.EnsureLoadedAsync();

        Assert.Equal(1, _service.GetCount);
    }

    [Fact]
    public async Task SaveAsync_WhileTheEditIsBeingSentAndReadBack_RefusesFurtherEdits_AndAllowsThemAgainOnceItFinishes()
    {
        await _session.EnsureLoadedAsync();
        var gate = new TaskCompletionSource();
        _service.PutGate = gate;
        _service.AcceptsPut("\"REVISION-2\"").ReturnsDocument(Document(), "\"REVISION-2\"");
        BindHttpRequest();

        var saveTask = _session.SaveAsync();
        var documentBeingSaved = (JsonObject)_session.Document!.DeepClone();

        Assert.True(_session.IsSaving);
        var refusedBinding = BpmnActivityBindingFormat.Create("Elsa.WriteLine", []);
        Assert.False(_session.SetBinding(TaskId, refusedBinding));
        Assert.True(JsonNode.DeepEquals(documentBeingSaved, _session.Document));

        gate.SetResult();
        var result = await saveTask;

        Assert.True(result.IsSuccess);
        Assert.False(_session.IsSaving);
        Assert.False(_session.IsDirty);

        // Refused while busy, not silently accepted and then lost to the reload: the session takes an edit again now.
        var binding = BindHttpRequest();
        Assert.True(JsonNode.DeepEquals(binding, BpmnActivityBindingFormat.Find(BpmnDocumentFixtures.TaskElement(_session.Document!))));
    }

    [Fact]
    public async Task SaveAsync_WhenThePutSucceedsButTheFollowUpReadFails_ReportsItSavedButNotReloaded_RatherThanJustSucceeding()
    {
        await _session.EnsureLoadedAsync();
        _service.AcceptsPut("\"REVISION-2\"").RefusesGet(BpmnDocumentFailureReason.SourceStale);
        BindHttpRequest();

        var result = await _session.SaveAsync();

        Assert.True(result.IsSuccess);
        Assert.True(_session.SavedButReloadFailed);
        Assert.Equal(BpmnDocumentFailureReason.SourceStale, _session.LoadFailure!.Reason);
        Assert.Null(_session.SaveFailure);
    }

    [Fact]
    public async Task RefreshRevisionAfterOrdinarySaveAsync_WhenTheContentIsUnchanged_AdoptsTheNewETag_SoTheSaveGoesThrough()
    {
        await _session.EnsureLoadedAsync();
        BindHttpRequest();
        _service.ReturnsDocument(Document(), "\"REVISION-2\"").AcceptsPut("\"REVISION-3\"").ReturnsDocument(Document(), "\"REVISION-3\"");

        var refreshed = await _session.RefreshRevisionAfterOrdinarySaveAsync();
        Assert.True(refreshed);

        var result = await _session.SaveAsync();

        Assert.True(result.IsSuccess);
        var put = Assert.Single(_service.Puts);
        Assert.Equal("\"REVISION-2\"", put.IfMatch);
    }

    [Fact]
    public async Task RefreshRevisionAfterOrdinarySaveAsync_WhenTheContentChanged_ReportsTheConflict_WithoutSendingAnything()
    {
        await _session.EnsureLoadedAsync();
        BindHttpRequest();
        var changedOnTheServer = Document();
        BpmnDocumentFixtures.TaskElement(changedOnTheServer)["name"] = "Renamed on the server";
        _service.ReturnsDocument(changedOnTheServer, "\"REVISION-2\"");

        var refreshed = await _session.RefreshRevisionAfterOrdinarySaveAsync();

        Assert.False(refreshed);
        Assert.Equal(BpmnDocumentFailureReason.PreconditionFailed, _session.SaveFailure!.Reason);
        Assert.Empty(_service.Puts);
        Assert.True(_session.IsDirty);
    }

    private JsonObject BindHttpRequest()
    {
        var binding = BpmnActivityBindingFormat.Create("Elsa.HttpRequest", [KeyValuePair.Create<string, JsonNode?>("url", JsonNode.Parse("""{"typeName":"Uri","expression":{"type":"JavaScript","value":"getUrl()"}}"""))]);
        _session.SetBinding(TaskId, binding);
        return binding;
    }
}
