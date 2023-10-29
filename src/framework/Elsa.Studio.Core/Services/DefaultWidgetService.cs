using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public class DefaultWidgetService : IWidgetService
{
    private readonly IEnumerable<IWidget> _widgets;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultWidgetService"/> class.
    /// </summary>
    /// <param name="widgets"></param>
    public DefaultWidgetService(IEnumerable<IWidget> widgets)
    {
        _widgets = widgets;
    }
    
    /// <inheritdoc />
    public IEnumerable<IWidget> GetWidgets(string zone)
    {
        return _widgets.Where(x => x.Zone == zone).OrderBy(x => x.Order).ToList();
    }
}