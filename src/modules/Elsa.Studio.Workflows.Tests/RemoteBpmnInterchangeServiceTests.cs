using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Client;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Domain.Services;
using Refit;
using Xunit;

namespace Elsa.Studio.Workflows.Tests;

/// <summary>
/// Covers <see cref="RemoteBpmnInterchangeService"/>'s error mapping: the server's 400/422 payloads carry the
/// message a user needs to see (capability names, offending element ids, or one of the two export refusals), so
/// this pins that those messages actually reach the caller rather than being replaced by a generic failure.
/// </summary>
public class RemoteBpmnInterchangeServiceTests : IDisposable
{
    private readonly FakeBpmnInterchangeApi _api = new();
    private readonly RemoteBpmnInterchangeService _service;

    public RemoteBpmnInterchangeServiceTests()
    {
        _service = new(new TestBackendApiClientProvider(_api));
    }

    public void Dispose() => _api.Dispose();

    [Fact]
    public async Task AnalyzeAsync_ReturnsTheServersErrorMessage_WhenTheApiThrows()
    {
        _api.AnalyzeException = await CreateApiExceptionAsync(HttpStatusCode.BadRequest, """
        {
          "errors": {
            "generalErrors": ["Upload exactly one .bpmn file."]
          }
        }
        """);

        using var stream = new MemoryStream();
        var result = await _service.AnalyzeAsync(stream, "process.bpmn");

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Failure!.Errors);
        Assert.Equal("Upload exactly one .bpmn file.", error.ErrorMessage);
    }

    [Fact]
    public async Task ImportAsync_ReturnsTheCapabilityRefusalMessage_On422()
    {
        const string message =
            "This deployment does not declare the following BPMN host capabilities the document requires: ScopeSignalling. "
            + "Offending elements (combined across all missing capabilities above, not attributable to any one of them): Gateway_1.";

        _api.ImportException = await CreateApiExceptionAsync(HttpStatusCode.UnprocessableEntity, $$"""
        {
          "errors": {
            "generalErrors": ["{{message}}"]
          }
        }
        """);

        using var stream = new MemoryStream();
        var result = await _service.ImportAsync(stream, "process.bpmn", definitionId: null, name: null, processId: null);

        Assert.True(result.IsFailed);
        var error = Assert.Single(result.Failure!.Errors);
        Assert.Equal(message, error.ErrorMessage);
    }

    [Fact]
    public async Task ExportAsync_ReturnsNotFound_WhenTheDefinitionDoesNotExist()
    {
        _api.ExportResponse = new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("") };

        var result = await _service.ExportAsync("missing-definition");

        Assert.True(result.IsFailed);
        Assert.Equal(BpmnExportFailureReason.NotFound, result.Failure!.Reason);
    }

    [Fact]
    public async Task ExportAsync_ClassifiesNotImportedFromBpmn()
    {
        const string message = "Workflow definition 'wf-1' does not currently carry BPMN source, so it cannot be exported as BPMN 2.0 XML.";
        _api.ExportResponse = UnprocessableEntity(message);

        var result = await _service.ExportAsync("wf-1");

        Assert.True(result.IsFailed);
        Assert.Equal(BpmnExportFailureReason.NotImportedFromBpmn, result.Failure!.Reason);
        Assert.Equal(message, result.Failure.Message);
    }

    [Fact]
    public async Task ExportAsync_ClassifiesDefinitionChangedSinceImport()
    {
        const string message = "Workflow definition 'wf-1' has changed since it was imported from BPMN (imported at version 1, currently at version 2).";
        _api.ExportResponse = UnprocessableEntity(message);

        var result = await _service.ExportAsync("wf-1");

        Assert.True(result.IsFailed);
        Assert.Equal(BpmnExportFailureReason.DefinitionChangedSinceImport, result.Failure!.Reason);
    }

    [Fact]
    public async Task ExportAsync_ReturnsTheDownload_OnSuccess()
    {
        const string xml = "<definitions />";
        _api.ExportResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes(xml))
        };

        var result = await _service.ExportAsync("wf-1");

        Assert.True(result.IsSuccess);
        Assert.Equal("wf-1.bpmn", result.Success!.FileName);
        using var reader = new StreamReader(result.Success.Content);
        Assert.Equal(xml, await reader.ReadToEndAsync());
    }

    private static HttpResponseMessage UnprocessableEntity(string message) => new(HttpStatusCode.UnprocessableEntity)
    {
        Content = new StringContent($$"""
        {
          "errors": {
            "generalErrors": ["{{message}}"]
          }
        }
        """)
    };

    private static async Task<ApiException> CreateApiExceptionAsync(HttpStatusCode statusCode, string content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/bpmn/analyze");
        using var response = new HttpResponseMessage(statusCode)
        {
            RequestMessage = request,
            Content = new StringContent(content)
        };

        return await ApiException.Create(request, HttpMethod.Post, response, new RefitSettings());
    }

    private class FakeBpmnInterchangeApi : IBpmnInterchangeApi, IDisposable
    {
        private readonly HttpResponseMessage _defaultExportResponse = new(HttpStatusCode.OK) { Content = new StringContent("") };

        public ApiException? AnalyzeException { get; set; }
        public ApiException? ImportException { get; set; }
        public HttpResponseMessage? ExportResponse { get; set; }

        public Task<BpmnImportAnalysisModel> AnalyzeAsync(StreamPart file, CancellationToken cancellationToken = default) =>
            AnalyzeException != null ? throw AnalyzeException : Task.FromResult(new BpmnImportAnalysisModel());

        public Task<BpmnImportResultModel> ImportAsync(StreamPart file, string? definitionId, string? name, string? processId, CancellationToken cancellationToken = default) =>
            ImportException != null ? throw ImportException : Task.FromResult(new BpmnImportResultModel());

        public Task<HttpResponseMessage> ExportAsync(string definitionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(ExportResponse ?? _defaultExportResponse);

        public void Dispose()
        {
            _defaultExportResponse.Dispose();
            ExportResponse?.Dispose();
        }
    }

    private class TestBackendApiClientProvider(IBpmnInterchangeApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://localhost");

        public ValueTask<T> GetApiAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(CancellationToken cancellationToken = default) where T : class
        {
            if (typeof(T) == typeof(IBpmnInterchangeApi))
                return ValueTask.FromResult((T)api);

            throw new NotSupportedException();
        }
    }
}
