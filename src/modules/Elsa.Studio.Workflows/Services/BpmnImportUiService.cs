using Elsa.Api.Client.Shared.Models;
using Elsa.Studio.Localization;
using Elsa.Studio.Workflows.Components.WorkflowDefinitionList;
using Elsa.Studio.Workflows.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Models;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace Elsa.Studio.Workflows.Services;

/// <inheritdoc cref="IBpmnImportUiService" />
public class BpmnImportUiService(
    IDialogService dialogService,
    ILocalizer localizer,
    IBpmnInterchangeService bpmnInterchangeService,
    IWorkflowDefinitionService workflowDefinitionService) : IBpmnImportUiService
{
    /// <summary>The maximum file size this flow reads into memory, matching <see cref="ImportOptions"/>'s own default.</summary>
    private const int MaxAllowedSize = 1024 * 1024 * 10; // 10 MB

    /// <inheritdoc />
    public bool IsBpmnFile(IBrowserFile file) => IsBpmnFileName(file.Name);

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="fileName"/> has a <c>.bpmn</c> extension, case-insensitively.
    /// </summary>
    public static bool IsBpmnFileName(string fileName) => fileName.EndsWith(".bpmn", StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<WorkflowImportResult?> ImportFileAsync(IBrowserFile file, string? definitionId, CancellationToken cancellationToken = default)
    {
        // The document is read once and re-used for both calls: Analyze and Import read the same file through the
        // same reader in elsa-core (see BpmnInterchangeDocumentService's remarks), so re-uploading whatever is still
        // on disk for the second call risks it having changed between the two round trips.
        await using var browserStream = file.OpenReadStream(MaxAllowedSize, cancellationToken);
        using var content = new MemoryStream();
        await browserStream.CopyToAsync(content, cancellationToken);

        content.Seek(0, SeekOrigin.Begin);
        var analysisResult = await bpmnInterchangeService.AnalyzeAsync(content, file.Name, cancellationToken);

        if (!analysisResult.IsSuccess)
            return Failed(file.Name, analysisResult.Failure!);

        var processId = await ShowFindingsDialogAsync(analysisResult.Success!);

        if (processId.Cancelled)
            return null;

        content.Seek(0, SeekOrigin.Begin);
        var importResult = await bpmnInterchangeService.ImportAsync(content, file.Name, definitionId, name: null, processId.ProcessId, cancellationToken);

        if (!importResult.IsSuccess)
            return Failed(file.Name, importResult.Failure!);

        var workflowDefinition = await workflowDefinitionService.FindByDefinitionIdAsync(importResult.Success!.DefinitionId, VersionOptions.Latest, cancellationToken);

        return new()
        {
            FileName = file.Name,
            WorkflowDefinition = workflowDefinition
        };
    }

    private async Task<(bool Cancelled, string? ProcessId)> ShowFindingsDialogAsync(BpmnImportAnalysisModel analysis)
    {
        var parameters = new DialogParameters<BpmnImportFindingsDialog> { { x => x.Analysis, analysis } };
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            Position = DialogPosition.Center,
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true
        };

        var dialog = await dialogService.ShowAsync<BpmnImportFindingsDialog>(localizer["Import BPMN"], parameters, options);
        var result = await dialog.Result;

        if (result?.Canceled != false)
            return (true, null);

        return (false, result.Data as string);
    }

    private static WorkflowImportResult Failed(string fileName, ValidationErrors errors) => new()
    {
        FileName = fileName,
        Failure = new(string.Join(" ", errors.Errors.Select(error => error.ErrorMessage)), WorkflowImportFailureType.Exception)
    };
}
