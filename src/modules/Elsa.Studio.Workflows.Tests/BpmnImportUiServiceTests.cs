using Bunit;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Requests;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Responses;
using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Services;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="BpmnImportUiService"/>'s per-file import flow. <see cref="ImportAsync_ReturnsAFailedResult_WhenTheImportedDefinitionCannotBeFound"/>
/// pins that a successful import whose definition cannot be re-fetched is reported as a failure rather than as a
/// success carrying a null <see cref="WorkflowImportResult.WorkflowDefinition"/>, which is what its two callers
/// dereference unconditionally.
/// </summary>
public sealed class BpmnImportUiServiceTests : BunitContext, IAsyncLifetime
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;

    public BpmnImportUiServiceTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<ILocalizer, TestLocalizer>();
        Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task ImportAsync_ReturnsAFailedResult_WhenTheImportedDefinitionCannotBeFound()
    {
        var dialogService = Services.GetRequiredService<IDialogService>();
        var localizer = Services.GetRequiredService<ILocalizer>();
        var interchangeService = new FakeBpmnInterchangeService
        {
            AnalyzeResult = new(new BpmnImportAnalysisModel { ProcessIds = ["only-process"] }),
            ImportResult = new(new BpmnImportResultModel { DefinitionId = "wf-1", Version = 1 })
        };
        var definitionService = new FakeWorkflowDefinitionService { DefinitionToReturn = null };
        var service = new BpmnImportUiService(dialogService, localizer, interchangeService, definitionService);
        var file = new FakeBrowserFile("process.bpmn");

        var importTask = _dialogProvider.InvokeAsync(() => service.ImportFileAsync(file, definitionId: null));
        _dialogProvider.WaitForElement("button");
        await _dialogProvider.InvokeAsync(() => ImportButton().ClickAsync(new MouseEventArgs()));

        var result = await importTask.WaitAsync(Timeout);

        Assert.NotNull(result);
        Assert.False(result!.IsSuccess);
        Assert.Null(result.WorkflowDefinition);
        Assert.Equal(WorkflowImportFailureType.Exception, result.Failure!.FailureType);
        Assert.Equal("wf-1", definitionService.RequestedDefinitionId);
    }

    private AngleSharp.Dom.IElement ImportButton() => _dialogProvider.FindAll("button").Single(x => x.TextContent.Trim() == "Import");

    private sealed class FakeBrowserFile(string name) : IBrowserFile
    {
        public string Name { get; } = name;
        public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
        public long Size => 0;
        public string ContentType => "application/xml";
        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default) => new MemoryStream();
    }

    private sealed class FakeBpmnInterchangeService : IBpmnInterchangeService
    {
        public Result<BpmnImportAnalysisModel, ValidationErrors> AnalyzeResult { get; set; } = new(new BpmnImportAnalysisModel());
        public Result<BpmnImportResultModel, ValidationErrors> ImportResult { get; set; } = new(new BpmnImportResultModel());

        public Task<Result<BpmnImportAnalysisModel, ValidationErrors>> AnalyzeAsync(Stream content, string fileName, CancellationToken cancellationToken = default) =>
            Task.FromResult(AnalyzeResult);

        public Task<Result<BpmnImportResultModel, ValidationErrors>> ImportAsync(Stream content, string fileName, string? definitionId, string? name, string? processId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ImportResult);

        public Task<Result<FileDownload, BpmnExportFailure>> ExportAsync(string definitionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// A stub that only implements <see cref="FindByDefinitionIdAsync"/>; every other member throws to flag an
    /// unexpected call.
    /// </summary>
    private sealed class FakeWorkflowDefinitionService : IWorkflowDefinitionService
    {
        public WorkflowDefinition? DefinitionToReturn { get; set; }
        public string? RequestedDefinitionId { get; private set; }

        public Task<WorkflowDefinition?> FindByDefinitionIdAsync(string definitionId, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default)
        {
            RequestedDefinitionId = definitionId;
            return Task.FromResult(DefinitionToReturn);
        }

        public Task<PagedListResponse<WorkflowDefinitionSummary>> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
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

    private sealed class TestLocalizer : ILocalizer
    {
        public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
        public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
    }
}
