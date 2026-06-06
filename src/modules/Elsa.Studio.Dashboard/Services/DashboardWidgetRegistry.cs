using Elsa.Studio.Dashboard.Models;

namespace Elsa.Studio.Dashboard.Services;

public class DashboardWidgetRegistry : IDashboardWidgetRegistry
{
    private readonly List<DashboardWidgetDescriptor> _descriptors = [];

    public void Add(DashboardWidgetDescriptor descriptor)
    {
        DashboardWidgetValidator.Validate(descriptor);

        if (_descriptors.Any(x => string.Equals(x.Id, descriptor.Id, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Dashboard widget descriptor '{descriptor.Id}' is already registered.");

        _descriptors.Add(descriptor);
    }

    public IReadOnlyCollection<DashboardWidgetDescriptor> List() => _descriptors.ToList();
}
