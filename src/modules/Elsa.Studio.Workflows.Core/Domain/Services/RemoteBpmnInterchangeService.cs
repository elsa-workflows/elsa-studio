using System.Net;
using Elsa.Studio.Contracts;
using Elsa.Studio.Models;
using Elsa.Studio.Workflows.Client;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Extensions;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Refit;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <inheritdoc />
public class RemoteBpmnInterchangeService(IBackendApiClientProvider backendApiClientProvider) : IBpmnInterchangeService
{
    /// <inheritdoc />
    public async Task<Result<BpmnImportAnalysisModel, ValidationErrors>> AnalyzeAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var file = new StreamPart(content, fileName, "application/xml");

        try
        {
            var analysis = await api.AnalyzeAsync(file, cancellationToken);
            return new(analysis);
        }
        catch (ApiException e)
        {
            return new(e.GetValidationErrors());
        }
    }

    /// <inheritdoc />
    public async Task<Result<BpmnImportResultModel, ValidationErrors>> ImportAsync(
        Stream content,
        string fileName,
        string? definitionId,
        string? name,
        string? processId,
        CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var file = new StreamPart(content, fileName, "application/xml");

        try
        {
            var result = await api.ImportAsync(file, definitionId, name, processId, cancellationToken);
            return new(result);
        }
        catch (ApiException e)
        {
            return new(e.GetValidationErrors());
        }
    }

    /// <inheritdoc />
    public async Task<Result<FileDownload, BpmnExportFailure>> ExportAsync(string definitionId, CancellationToken cancellationToken = default)
    {
        var api = await GetApiAsync(cancellationToken);
        var response = await api.ExportAsync(definitionId, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return new(new FileDownload($"{definitionId}.bpmn", stream));
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new(new BpmnExportFailure(BpmnExportFailureReason.NotFound, "Workflow definition not found."));

        var message = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(body)?.Errors.FirstOrDefault()?.ErrorMessage
            ?? (string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "The export could not be completed." : body);

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            return new(new BpmnExportFailure(ClassifyExportRefusal(message), message));

        return new(new BpmnExportFailure(BpmnExportFailureReason.Unknown, message));
    }

    /// <summary>
    /// Classifies the two 422 refusals <c>BpmnInterchangeDocumentService.Export(WorkflowDefinition)</c> raises by
    /// matching the distinguishing phrase each one carries, so the UI can show its own short wording instead of the
    /// full diagnostic sentence. See that method's remarks in elsa-core for why each refusal reads the way it does.
    /// </summary>
    private static BpmnExportFailureReason ClassifyExportRefusal(string message)
    {
        if (message.Contains("has changed since it was imported from BPMN", StringComparison.OrdinalIgnoreCase))
            return BpmnExportFailureReason.DefinitionChangedSinceImport;

        if (message.Contains("does not currently carry BPMN source", StringComparison.OrdinalIgnoreCase)
            || message.Contains("not the definition version it was recorded against", StringComparison.OrdinalIgnoreCase))
            return BpmnExportFailureReason.NotImportedFromBpmn;

        return BpmnExportFailureReason.Unknown;
    }

    private async Task<IBpmnInterchangeApi> GetApiAsync(CancellationToken cancellationToken = default) =>
        await backendApiClientProvider.GetApiAsync<IBpmnInterchangeApi>(cancellationToken);
}
