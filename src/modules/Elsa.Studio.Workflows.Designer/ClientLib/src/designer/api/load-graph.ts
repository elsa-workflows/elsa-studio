import {Graph, Model} from '@antv/x6';
import {graphBindings} from "./graph-bindings";
import {arrangeSequenceGraph, normalizeSequenceOrientation} from "./sequence-mode";

export function loadGraph(graphId: string, data: string | Model.FromJSONData) {
    const binding = graphBindings[graphId];
    const {graph} = binding;
    const model = typeof data === 'string' ? JSON.parse(data) : data;
    graph.fromJSON(model);

    if (binding.mode === 'sequence') {
        binding.layoutOrientation = normalizeSequenceOrientation((model as any).layoutOrientation);
        arrangeSequenceGraph(binding);
    }

    waitUntilCanvasHasNonZeroHeight(graph).then(() => graph.centerContent({padding: 20}));
}

function waitUntilCanvasHasNonZeroHeight(graph: Graph): Promise<void> {
    const container = graph.container;

    return new Promise((resolve, reject) => {
        const checkSize = () => {
            const clientRect = container.getBoundingClientRect();

            if (clientRect.height == 0)
                window.requestAnimationFrame(checkSize);
            else
                resolve();
        };

        checkSize();
    });
}
