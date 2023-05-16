using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;
using Elsa.Api.Client.Shared.Models;

namespace Elsa.Studio.Workflows.Contracts;

public interface IWorkflowDefinitionService
{
    Task<ListWorkflowDefinitionsResponse> ListAsync(ListWorkflowDefinitionsRequest request, VersionOptions? versionOptions = default, CancellationToken cancellationToken = default);
}