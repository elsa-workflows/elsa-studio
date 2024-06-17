using System.Text.Json.Nodes;
using Elsa.Api.Client.Resources.ActivityExecutions.Models;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// A service that provides information about the execution of activities.
/// </summary>
public interface IActivityExecutionService
{
    /// <summary>
    /// Gets a report of the execution of activities.
    /// </summary>
    Task<ActivityExecutionReport> GetReportAsync(string workflowInstanceId, JsonObject containerActivity, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a list of activity execution records.
    /// </summary>
    Task<IEnumerable<ActivityExecutionRecord>> ListAsync(string workflowInstanceId, string activityNodeId, CancellationToken cancellationToken = default);
}