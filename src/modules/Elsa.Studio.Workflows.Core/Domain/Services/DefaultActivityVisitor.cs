using System.Text.Json.Nodes;
using Elsa.Api.Client.Extensions;
using Elsa.Studio.Workflows.Domain.Contracts;

namespace Elsa.Studio.Workflows.Domain.Services;

public class DefaultActivityVisitor : IActivityVisitor
{
    public IEnumerable<JsonObject> Visit(JsonObject root)
    {
        var activities = new List<JsonObject>();

        foreach (var property in root)
        {
            var value = property.Value;
            if (value is JsonObject jsonObject)
            {
                activities.AddRange(ProcessJsonObject(jsonObject));
            }
            else if (value is JsonArray jsonArray)
            {
                foreach(var item in jsonArray)
                {
                    if (item is JsonObject jsonObjectInArray) 
                        activities.AddRange(ProcessJsonObject(jsonObjectInArray));
                }
            }
        }
        
        return activities.DistinctBy(x => x.GetId()).ToList();
    }

    private IEnumerable<JsonObject> ProcessJsonObject(JsonObject obj)
    {
        var activities = new List<JsonObject>();
        if (IsActivity(obj))
        {
            activities.Add(obj);
        }
        activities.AddRange(Visit(obj));
        return activities;
    }
    
    private static bool IsActivity(JsonObject obj)
    {
        return obj.ContainsKey("type") && obj.ContainsKey("id") && obj.ContainsKey("version");
    }
}