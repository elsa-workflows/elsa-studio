using System.IO.Compression;
using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Api.Client.Converters;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Contracts;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.Domain.Notifications;
using Microsoft.AspNetCore.Components.Forms;

namespace Elsa.Studio.Workflows.Domain.Services;

/// <summary>
/// A workflow definition service that uses a remote backend to retrieve workflow definitions.
/// </summary>
public class WorkflowDefinitionImporter(IRemoteBackendApiClientProvider remoteBackendApiClientProvider, IMediator mediator, IWorkflowJsonDetector workflowJsonDetector) : IWorkflowDefinitionImporter
{
    private async Task<IWorkflowDefinitionsApi> GetApiAsync(CancellationToken cancellationToken = default)
    {
        return await remoteBackendApiClientProvider.GetApiAsync<IWorkflowDefinitionsApi>(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<IBrowserFile>> ImportFilesAsync(IReadOnlyList<IBrowserFile> files, ImportOptions? options = null)
    {
        var importedFiles = new List<IBrowserFile>();
        var maxAllowedSize = options?.MaxAllowedSize ?? 1024 * 1024 * 10; // 10 MB

        foreach (var file in files)
        {
            await mediator.NotifyAsync(new ImportingFile(file));
            await using var stream = file.OpenReadStream(maxAllowedSize);

            if (file.ContentType == MediaTypeNames.Application.Zip || file.Name.EndsWith(".zip"))
            {
                var success = await ImportZipFileAsync(stream, options);
                if (success)
                {
                    importedFiles.Add(file);
                    await mediator.NotifyAsync(new ImportedFile(file));
                }
            }

            else if (file.ContentType == MediaTypeNames.Application.Json || file.Name.EndsWith(".json"))
            {
                var success = await ImportFromStreamAsync(stream, options);
                if (success)
                {
                    importedFiles.Add(file);
                    await mediator.NotifyAsync(new ImportedFile(file));
                }
            }
        }

        return importedFiles;
    }

    private async Task<bool> ImportZipFileAsync(Stream stream, ImportOptions? options)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        memoryStream.Seek(0, SeekOrigin.Begin);
        var zipArchive = new ZipArchive(memoryStream);

        foreach (var entry in zipArchive.Entries)
        {
            if (entry.FullName.EndsWith(".json"))
            {
                await using var entryStream = entry.Open();
                var success = await ImportFromStreamAsync(entryStream, options);

                if (success)
                    return true;
            }
            else if (entry.FullName.EndsWith(".zip"))
            {
                await using var entryStream = entry.Open();
                var success = await ImportZipFileAsync(entryStream, options);

                if (success)
                    return true;
            }
        }

        return false;
    }

    private async Task<bool> ImportFromStreamAsync(Stream stream, ImportOptions? options)
    {
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();
        var jsonSerializerOptions = CreateJsonSerializerOptions();

        try
        {
            await mediator.NotifyAsync(new ImportingJson(json));
            
            // Check if this is a workflow definition file.
            if(!workflowJsonDetector.IsWorkflowSchema(json))
                return true;
            
            var model = JsonSerializer.Deserialize<WorkflowDefinitionModel>(json, jsonSerializerOptions)!;
            
            if(options?.DefinitionId != null)
                model.DefinitionId = options.DefinitionId;
            
            await mediator.NotifyAsync(new WorkflowDefinitionImporting(model));
            var api = await GetApiAsync();
            var newWorkflowDefinition = await api.ImportAsync(model);
            
            if(options?.ImportedCallback != null)
                await options.ImportedCallback(newWorkflowDefinition);
            
            await mediator.NotifyAsync(new WorkflowDefinitionImported(newWorkflowDefinition));
            await mediator.NotifyAsync(new ImportedJson(json));
        }
        catch (Exception e)
        {
            if(options?.ErrorCallback != null)
                await options.ErrorCallback(e);
            return false;
        }

        return true;
    }
    
    private static JsonSerializerOptions CreateJsonSerializerOptions()
    {
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new VersionOptionsJsonConverter());
        return options;
    }
}

public class ImportOptions
{
    public int MaxAllowedSize { get; set; } = 1024 * 1024 * 10; // 10 MB
    public string? DefinitionId { get; set; }
    public Func<WorkflowDefinition, Task>? ImportedCallback { get; set; }
    public Func<Exception, Task> ErrorCallback { get; set; }
}