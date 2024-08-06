using Elsa.Api.Client.Extensions;
using Elsa.Studio.WorkflowContexts.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Elsa.Studio.WorkflowContexts.Extensions
{
    public static class ActivityExtensions
    {
        public static void SetWorkflowContextSettings(this JsonObject activity, Dictionary<string, ActivityWorkflowContextSettings> value) =>
            activity.SetProperty(JsonValue.Create(value), "customProperties", "ActivityWorkflowContextSettingsKey");

        public static Dictionary<string, ActivityWorkflowContextSettings> GetWorkflowContextSettings(this JsonObject activity) =>
            activity.GetProperty<Dictionary<string, ActivityWorkflowContextSettings>>("customProperties", "ActivityWorkflowContextSettingsKey");
    }
}
