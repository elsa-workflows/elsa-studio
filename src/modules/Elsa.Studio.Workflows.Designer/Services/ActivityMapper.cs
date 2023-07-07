using Elsa.Api.Client.Activities;
using Elsa.Api.Client.Extensions;
using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Designer.Contracts;
using Elsa.Studio.Workflows.Designer.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Contracts;
using Elsa.Studio.Workflows.UI.Contracts;

namespace Elsa.Studio.Workflows.Designer.Services;

internal class ActivityMapper : IActivityMapper
{
    private readonly IDictionary<string, ActivityDescriptor> _activityDescriptors;
    private readonly IActivityPortService _activityPortService;
    private readonly IActivityDisplaySettingsRegistry _activityDisplaySettingsRegistry;

    public ActivityMapper(IDictionary<string, ActivityDescriptor> activityDescriptors, IActivityPortService activityPortService, IActivityDisplaySettingsRegistry activityDisplaySettingsRegistry)
    {
        _activityDescriptors = activityDescriptors;
        _activityPortService = activityPortService;
        _activityDisplaySettingsRegistry = activityDisplaySettingsRegistry;
    }

    public X6Node MapActivity(Activity activity)
    {
        var activityId = activity.Id;
        var designerMetadata = activity.GetDesignerMetadata();
        var position = designerMetadata.Position;
        var size = designerMetadata.Size;
        var x = position?.X ?? 0;
        var y = position?.Y ?? 0;
        var width = size?.Width ?? 0;
        var height = size?.Height ?? 0;

        if (width == 0) width = 200;
        if (height == 0) height = 50;

        // Create node.
        var node = new X6Node
        {
            Id = activityId,
            Data = activity,
            Size = new X6Size(width, height),
            Position = new X6Position(x, y),
            Shape = "elsa-activity",
            Ports = GetPorts(activity)
        };

        return node;
    }

    public X6Ports GetPorts(Activity activity)
    {
        // Create input ports.
        var inPorts = GetInPorts(activity);

        // Create output ports.
        var outPorts = GetOutPorts(activity);

        // Concatenate input and output ports.
        return new X6Ports
        {
            Items = inPorts.Concat(outPorts).ToList()
        };
    }

    public IEnumerable<X6Port> GetOutPorts(Activity activity)
    {
        var activityType = activity.Type;
        var activityDescriptor = _activityDescriptors[activityType];
        var sourcePorts = _activityPortService.GetPorts(new PortProviderContext(activityDescriptor, activity)).Where(x => x.Type == PortType.Flow);
        var displaySettings = _activityDisplaySettingsRegistry.GetSettings(activity.Type);

        var ports = sourcePorts.Select(sourcePort => new X6Port
        {
            Id = sourcePort.Name,
            Group = "out",
            Attrs = new X6Attrs
            {
                ["text"] = new X6Attrs
                {
                    ["text"] = sourcePort.DisplayName ?? string.Empty
                }
            }
        }).ToList();

        if (ports.All(port => port.Group != "out"))
            ports.Add(new X6Port
            {
                Id = "Done",
                Group = "out",
                Attrs = new X6Attrs
                {
                    ["text"] = new X6Attrs
                    {
                        ["text"] = "Done"
                    },
                    ["circle"] = new X6Attrs
                    {
                        ["fill"] = displaySettings.Color,
                    }
                }
            });

        return ports;
    }

    public IEnumerable<X6Port> GetInPorts(Activity activity)
    {
        var displaySettings = _activityDisplaySettingsRegistry.GetSettings(activity.Type);

        // Create default input port.
        yield return new X6Port
        {
            Id = "In",
            Group = "in",
            Attrs = new X6Attrs
            {
                ["circle"] = new X6Attrs
                {
                    ["stroke"] = displaySettings.Color,
                }
            }
        };
    }
}