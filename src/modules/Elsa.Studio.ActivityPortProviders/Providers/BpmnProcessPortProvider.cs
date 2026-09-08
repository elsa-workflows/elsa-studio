using Elsa.Api.Client.Resources.ActivityDescriptors.Enums;
using Elsa.Api.Client.Resources.ActivityDescriptors.Models;
using Elsa.Studio.Workflows.Domain.Contexts;
using Elsa.Studio.Workflows.Domain.Models.Bpmn;
using Elsa.Studio.Workflows.Domain.Providers;

namespace Elsa.Studio.ActivityPortProviders.Providers;

/// <summary>
/// Provides the outcome ports of an <c>Elsa.BpmnProcess</c> node on a flowchart.
/// </summary>
/// <remarks>
/// <para>
/// elsa-core's <c>BpmnProcess</c> declares no <c>[FlowNode]</c> outcomes, so its descriptor carries no ports at all
/// and the node would otherwise fall back to the single synthesized port the flowchart mapper adds for an activity
/// with none. That fallback is not enough: a BPMN scope completes with the interpreter's outcome name only -- never
/// with <c>Outcomes.Default</c>, which is what makes an ordinary activity's null-port connection fire -- so the
/// <c>Cancelled</c> outcome would have no port to connect from at all.
/// </para>
/// <para>
/// The outcomes are <em>added</em> to whatever the descriptor already declares rather than substituted for it: a
/// provider replaces the default one outright, so returning only these two would silently hide any port elsa-core
/// declares later -- an embedded port that stopped rendering, with nothing to say why. Declaring the outcomes on the
/// activity in elsa-core would then make this provider a no-op superset and it could be deleted; that is an
/// elsa-core follow-up, not something Studio can do from here.
/// </para>
/// </remarks>
public class BpmnProcessPortProvider : ActivityPortProviderBase
{
    /// <inheritdoc />
    public override bool GetSupportsActivityType(PortProviderContext context) =>
        context.ActivityDescriptor.TypeName == BpmnProcessConstants.ActivityTypeName;

    /// <inheritdoc />
    public override IEnumerable<Port> GetPorts(PortProviderContext context)
    {
        var declaredPorts = context.ActivityDescriptor.Ports.ToList();

        foreach (var port in declaredPorts)
            yield return port;

        var declaredNames = declaredPorts.Select(x => x.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var outcomeName in BpmnProcessConstants.OutcomeNames.Where(x => !declaredNames.Contains(x)))
            yield return new()
            {
                Name = outcomeName,
                DisplayName = outcomeName,
                Type = PortType.Flow
            };
    }
}
