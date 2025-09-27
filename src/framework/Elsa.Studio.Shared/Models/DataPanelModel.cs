namespace Elsa.Studio.Models;

/// <summary>
/// Represents a data panel model.
/// </summary>
public class DataPanelModel : List<DataPanelItem>
{
    public DataPanelModel()
    {
    }

    public DataPanelModel(IEnumerable<DataPanelItem> collection) : base(collection)
    {
    }
    
    /// <summary>
    /// Provides the add.
    /// </summary>
    public void Add(string label, string? text = null, string? link = null) => Add(new DataPanelItem(label, text, link));
}

/// <summary>
/// Provides extension methods for data panel model linq.
/// </summary>
public static class DataPanelModelLinqExtensions
{
    /// <summary>
    /// Provides the to data panel model.
    /// </summary>
    public static DataPanelModel ToDataPanelModel(this IEnumerable<DataPanelItem> items) => new(items);
}