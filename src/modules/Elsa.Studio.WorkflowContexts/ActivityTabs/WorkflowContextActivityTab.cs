using Elsa.Studio.Contracts;
using Elsa.Studio.Localization;
using Elsa.Studio.WorkflowContexts.Components;
using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Studio.WorkflowContexts.ActivityTabs
{
    public class WorkflowContextActivityTab : IActivityTab
    {
        [Inject] private ILocalizer Localizer { get; set; } = default!;
        public string Title => Localizer["Workflow Context"];

        public Func<IDictionary<string, object?>, RenderFragment> Render => attributes => builder =>
        {
            builder.OpenComponent<WorkflowContextActivityTabPanel>(0);
            builder.AddAttribute(1, nameof(WorkflowContextActivityTabPanel.WorkflowDefinition), attributes["WorkflowDefinition"]);
            builder.AddAttribute(2, nameof(WorkflowContextActivityTabPanel.Activity), attributes["Activity"]);
            builder.AddAttribute(2, nameof(WorkflowContextActivityTabPanel.ActivityDescriptor), attributes["ActivityDescriptor"]);
            builder.AddAttribute(2, nameof(WorkflowContextActivityTabPanel.OnActivityUpdated), attributes["OnActivityUpdated"]);
            builder.CloseComponent();
        };

    }
}
