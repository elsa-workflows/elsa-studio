using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

public interface IActivityExecutionService
{
    Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default);
}