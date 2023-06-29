using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;

namespace Elsa.Studio.Workflows.Models;

/// <summary>
/// Represents the context for a port provider.
/// </summary>
/// <param name="ActivityDescriptor">The descriptor of te activity for which ports are being provided.</param> 
/// <param name="Activity">The activity for which ports are being provided.</param>
public record PortProviderContext(ActivityDescriptor ActivityDescriptor, Activity Activity);