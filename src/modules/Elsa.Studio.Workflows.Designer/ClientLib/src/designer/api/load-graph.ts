import {Graph, Model} from '@antv/x6';
import {graphBindings} from "./graph-bindings";
import {arrangeSequenceGraph, normalizeSequenceOrientation, withSuppressedGraphUpdated} from "./sequence-mode";
import {applyStateMachineGraphAccessibility} from '../internal/state-machine-accessibility';

export function loadGraph(graphId: string, data: string | Model.FromJSONData) {
    const binding = graphBindings[graphId];
    const {graph} = binding;
    const model = typeof data === 'string' ? JSON.parse(data) : data;
    withSuppressedGraphUpdated(binding, () => graph.fromJSON(model));

    if (binding.mode === 'sequence') {
        binding.layoutOrientation = normalizeSequenceOrientation((model as any).layoutOrientation);
        arrangeSequenceGraph(binding);
    }

    waitUntilCanvasHasNonZeroHeight(graphId, graph).then(ready => {
        if (!ready || graphBindings[graphId] !== binding)
            return;

        if (binding.mode === 'stateMachine')
            applyStateMachineGraphAccessibility(graph);

        graph.centerContent({padding: 20});
    });
}

function waitUntilCanvasHasNonZeroHeight(graphId: string, graph: Graph): Promise<boolean> {
    const container = graph.container;

    return new Promise(resolve => {
        const checkSize = () => {
            if (!container.isConnected || graphBindings[graphId]?.graph !== graph) {
                resolve(false);
                return;
            }

            const clientRect = container.getBoundingClientRect();

            if (clientRect.height == 0)
                window.requestAnimationFrame(checkSize);
            else
                resolve(true);
        };

        checkSize();
    });
}
