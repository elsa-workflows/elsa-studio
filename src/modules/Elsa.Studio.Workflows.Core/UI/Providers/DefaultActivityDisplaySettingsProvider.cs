using Elsa.Studio.Workflows.UI.Contracts;
using Elsa.Studio.Workflows.UI.Models;
using MudBlazor;

namespace Elsa.Studio.Workflows.UI.Providers;

/// <summary>
/// Provides default activity display settings.
/// </summary>
public class DefaultActivityDisplaySettingsProvider : IActivityDisplaySettingsProvider
{
    /// <inheritdoc />
    public IDictionary<string, ActivityDisplaySettings> GetSettings() => new Dictionary<string, ActivityDisplaySettings>
    {
        // Branching
        ["Elsa.If"] = new(DefaultActivityColors.Branching, ElsaStudioIcons.Heroicons.Question),
        ["Elsa.FlowDecision"] = new(DefaultActivityColors.Branching, ElsaStudioIcons.Heroicons.Question),
        ["Elsa.Switch"] = new(DefaultActivityColors.Branching, ElsaStudioIcons.Tabler.SwitchDiagonal),
        ["Elsa.FlowSwitch"] = new(DefaultActivityColors.Branching, ElsaStudioIcons.Tabler.SwitchDiagonal),
        ["Elsa.FlowJoin"] = new(DefaultActivityColors.Branching, ElsaStudioIcons.Tabler.GitMerge),
        
        // Composition
        ["Elsa.Complete"] = new(DefaultActivityColors.Composition, ElsaStudioIcons.Tabler.CheckCircle),
        ["Elsa.SetOutput"] = new (DefaultActivityColors.Composition),
        ["Elsa.DispatchWorkflow"] = new (DefaultActivityColors.Composition),
        
        // Console
        ["Elsa.WriteLine"] = new(DefaultActivityColors.Console, ElsaStudioIcons.Tabler.Pencil),
        ["Elsa.ReadLine"] = new(DefaultActivityColors.Console, ElsaStudioIcons.Tabler.Text),
        
        // Email
        ["Elsa.SendEmail"] = new(DefaultActivityColors.Email, Icons.Material.Outlined.Email),
        
        // Flowchart
        ["Elsa.Flowchart"] = new(DefaultActivityColors.Flowchart, ElsaStudioIcons.Tabler.GitFork),
        ["Elsa.FlowNode"] = new(DefaultActivityColors.Flowchart, ElsaStudioIcons.Tabler.Hexagon),
        ["Elsa.Start"] = new(DefaultActivityColors.Flowchart, Icons.Material.Outlined.Start),
        ["Elsa.End"] = new(DefaultActivityColors.Flowchart, Icons.Material.Outlined.OutlinedFlag),
        
        // HTTP
        ["Elsa.HttpEndpoint"] = new(DefaultActivityColors.Http, ElsaStudioIcons.Tabler.Cloud),
        ["Elsa.WriteHttpResponse"] = new(DefaultActivityColors.Http, ElsaStudioIcons.Heroicons.PencilPaper),
        ["Elsa.SendHttpRequest"] = new(DefaultActivityColors.Http, ElsaStudioIcons.Tabler.World),
        ["Elsa.FlowSendHttpRequest"] = new(DefaultActivityColors.Http, ElsaStudioIcons.Tabler.World),
        
        // Looping
        ["Elsa.While"] = new(DefaultActivityColors.Looping, ElsaStudioIcons.Tabler.RepeatOne),
        ["Elsa.ForEach"] = new(DefaultActivityColors.Looping, ElsaStudioIcons.Tabler.RepeatOne),
        ["Elsa.For"] = new(DefaultActivityColors.Looping, ElsaStudioIcons.Tabler.RepeatOne),
        ["Elsa.Break"] = new(DefaultActivityColors.Looping, ElsaStudioIcons.Tabler.Back1),
        
        // Primitives
        ["Elsa.SetVariable"] = new(DefaultActivityColors.Primitives, ElsaStudioIcons.Tabler.Pencil),
        ["Elsa.SetName"] = new(DefaultActivityColors.Primitives, ElsaStudioIcons.Tabler.Italic),
        ["Elsa.Finish"] = new(DefaultActivityColors.Primitives, ElsaStudioIcons.Tabler.CheckShield),
        ["Elsa.Fault"] = new(DefaultActivityColors.Primitives, Icons.Material.Outlined.ErrorOutline),
        ["Elsa.Correlate"] = new(DefaultActivityColors.Primitives, Icons.Material.Outlined.DatasetLinked),
        ["Elsa.RunTask"] = new(DefaultActivityColors.Primitives, Icons.Material.Outlined.Settings),
        ["Elsa.PublishEvent"] = new(DefaultActivityColors.Primitives, Icons.Material.Outlined.FlashOn),
        ["Elsa.Event"] = new(DefaultActivityColors.Primitives, Icons.Material.Outlined.FlashOn),
        
        // Timers
        ["Elsa.Timer"] = new(DefaultActivityColors.Timer, Icons.Material.Outlined.Timer),
        ["Elsa.Cron"] = new(DefaultActivityColors.Timer, Icons.Material.Outlined.Timer),
        ["Elsa.Delay"] = new(DefaultActivityColors.Timer, Icons.Material.Outlined.RotateLeft),
        ["Elsa.StartAt"] = new(DefaultActivityColors.Timer, Icons.Material.Outlined.CalendarMonth),
        
        // Scripting
        ["Elsa.RunJavaScript"] = new(DefaultActivityColors.Scripting, Icons.Material.Outlined.Javascript),
    };
}