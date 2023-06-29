using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Models;

namespace Elsa.Studio.Workflows.Contracts;

/// <summary>
/// Provides ports for activities.
/// </summary>
public interface IPortProvider
{
    /// <summary>
    /// Gets the priority of the provider.
    /// </summary>
    double Priority { get; }
    
    /// <summary>
    /// Returns true if the provider supports the specified activity type.
    /// </summary>
    /// <param name="activityType">The type of the activity.</param>
    bool GetSupportsActivityType(string activityType);
    
    /// <summary>
    /// Returns the ports for the specified activity.
    /// </summary>
    /// <param name="context">The context.</param>
    IEnumerable<Port> GetPorts(PortProviderContext context);
    
    /// <summary>
    /// Returns the activity for the specified port.
    /// </summary>
    /// <param name="portName">The name of the port.</param>
    /// <param name="context">The context.</param>
    Activities? ResolvePort(string portName, PortProviderContext context);
    
    /// <summary>
    /// Assigns the specified activity to the specified port.
    /// </summary>
    /// <param name="portName">The name of the port.</param>
    /// <param name="activities">The activity to assign.</param>
    /// <param name="context">The context.</param>
    void AssignPort(string portName, Activities? activities, PortProviderContext context);
}