import {Graph, Node} from '@antv/x6';

const AccessibilityMarker = 'data-elsa-state-machine-a11y';

export function applyStateMachineGraphAccessibility(graph: Graph) {
    graph.getNodes().forEach(node => applyStateMachineNodeAccessibility(graph, node));
}

export function applyStateMachineNodeAccessibility(graph: Graph, node: Node) {
    const view = graph.findViewByCell(node);
    const accessibleName = node.getData()?.accessibleName
        ?? `${node.attr('title/text') || 'Unnamed'}, state`;
    const container = view?.container
        ?? graph.container.querySelector<SVGGElement>(`.x6-node[data-cell-id="${CSS.escape(node.id)}"]`);
    if (!container || !accessibleName)
        return;

    container.setAttribute('role', 'button');
    container.setAttribute('aria-label', accessibleName);
    container.setAttribute('tabindex', '0');

    if (container.getAttribute(AccessibilityMarker) === 'true')
        return;

    container.setAttribute(AccessibilityMarker, 'true');
    container.addEventListener('keydown', event => {
        const keyboardEvent = event as KeyboardEvent;
        if (keyboardEvent.key !== 'Enter' && keyboardEvent.key !== ' ')
            return;

        keyboardEvent.preventDefault();
        graph.select(node);
    });
}
