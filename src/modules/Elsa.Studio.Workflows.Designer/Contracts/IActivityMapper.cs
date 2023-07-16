using System.Text.Json.Nodes;
using Elsa.Studio.Workflows.Designer.Models;

namespace Elsa.Studio.Workflows.Designer.Contracts;

/// <summary>
/// Maps an Activity to an X6Node.
/// </summary>
public interface IActivityMapper
{
    /// <summary>
    /// Maps an activity to an X6Node.
    /// </summary>
    /// <param name="activity">The activity.</param>
    X6Node MapActivity(JsonObject activity);
    
    /// <summary>
    /// Gets the outbound ports for an activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    IEnumerable<X6Port> GetOutPorts(JsonObject activity);
    
    /// <summary>
    /// Gets the inbound ports for an activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    IEnumerable<X6Port> GetInPorts(JsonObject activity);
    
    /// <summary>
    /// Gets the inbound and outbound ports for an activity.
    /// </summary>
    /// <param name="activity">The activity.</param>
    X6Ports GetPorts(JsonObject activity);
}