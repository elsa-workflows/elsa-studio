namespace Elsa.Studio.Contracts;

/// <summary>
/// Provides widgets.
/// </summary>
public interface IWidgetService
{
    /// <summary>
    /// Gets the widgets for the specified zone.
    /// </summary>
    IEnumerable<IWidget> GetWidgets(string zone);
}