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

        var validationErrors = ValidationApiExceptionExtensions.GetValidationErrorsFromContent(body);
        var message = validationErrors?.Errors.FirstOrDefault()?.ErrorMessage
            ?? (string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "The export could not be completed." : body);

        if (response.StatusCode == HttpStatusCode.UnprocessableEntity)
            return new(new BpmnExportFailure(ClassifyExportRefusal(validationErrors?.Code), message));

        return new(new BpmnExportFailure(BpmnExportFailureReason.Unknown, message));
    }

    /// <summary>
    /// Classifies the 422 refusals <c>BpmnInterchangeDocumentService.Export(WorkflowDefinition)</c> raises by their
    /// machine-readable <paramref name="code"/> (see <see cref="BpmnErrorCodes"/>), rather than by matching a phrase
    /// in the message, so a rewording of the message never breaks this classification. A <see langword="null"/> or
    /// unrecognized code — including an older server that does not send one yet — falls back to
    /// <see cref="BpmnExportFailureReason.Unknown"/>, which shows the server's own message.
    /// </summary>
    private static BpmnExportFailureReason ClassifyExportRefusal(string? code) => code switch
    {
        BpmnErrorCodes.ExportNotImported => BpmnExportFailureReason.NotImportedFromBpmn,
        // Not reachable through Import or the document PUT themselves (both always record a source version
        // alongside the source text); Studio has no wording of its own for this case, distinct from
        // ExportNotImported's, so it maps to the same "not imported" message.
        BpmnErrorCodes.ExportSourceVersionUnknown => BpmnExportFailureReason.NotImportedFromBpmn,
        BpmnErrorCodes.ExportSourceStale => BpmnExportFailureReason.DefinitionChangedSinceImport,
        _ => BpmnExportFailureReason.Unknown
    };

    private async Task<IBpmnInterchangeApi> GetApiAsync(CancellationToken cancellationToken = default) =>
        await backendApiClientProvider.GetApiAsync<IBpmnInterchangeApi>(cancellationToken);
}
