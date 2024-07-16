import {Graph, Model} from '@antv/x6';
import {graphBindings} from "./graph-bindings";

export function loadGraph(graphId: string, data: string | Model.FromJSONData) {
    const {graph} = graphBindings[graphId];
    const model = typeof data === 'string' ? JSON.parse(data) : data;
    graph.fromJSON(model);

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