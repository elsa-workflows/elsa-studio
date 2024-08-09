// using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
// using Elsa.Studio.Workflows.Domain.Contracts;
// using Elsa.Studio.Workflows.UI.Contracts;
// using Elsa.Studio.Workflows.UI.Models;
// using MudBlazor;
//
// namespace Elsa.Studio.Agents.UI.Providers;
//
// /// <summary>
// /// Provides default activity display settings.
// /// </summary>
// public class AgentsActivityDisplaySettingsProvider(IActivityRegistry activityRegistry) : IActivityDisplaySettingsProvider
// {
//     /// <inheritdoc />
//     public IDictionary<string, ActivityDisplaySettings> GetSettings()
//     {
//         var agentActivityDescriptors = activityRegistry.List()
//             .Where(x => x.CustomProperties.TryGetValue("RootType", out var rootType) && rootType as string == "AgentActivity")
//             .ToList();
//         
//         return agentActivityDescriptors.ToDictionary(x => x.TypeName, x => new ActivityDisplaySettings("#03fcad", ))
//         
//         return new Dictionary<string, ActivityDisplaySettings>
//         {
//             
//
//             // Agents Activities
//             ["Elsa.NotFoundActivity"] = new(DefaultActivityColors.NotFound, ElsaStudioIcons.Heroicons.Exclamation)
//         };
//     }
// }