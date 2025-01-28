namespace Elsa.Studio.Models;

public class DataPanelModel : List<DataPanelItem>
{
    public DataPanelModel()
    {
    }

    public DataPanelModel(IEnumerable<DataPanelItem> collection) : base(collection)
    {
    }
    
    public void Add(string label, string? text = null, string? link = null) => Add(new DataPanelItem(label, text, link));
}

public static class DataPanelModelLinqExtensions
{
    public static DataPanelModel ToDataPanelModel(this IEnumerable<DataPanelItem> items) => new(items);
}