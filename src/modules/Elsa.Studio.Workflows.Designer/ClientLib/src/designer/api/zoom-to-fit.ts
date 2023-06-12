import {Model, Graph} from '@antv/x6';
import {graphs} from "../internal/graphs";

export function zoomToFit(graphId: string) {
    const graph: Graph = graphs[graphId];
    graph.zoomToFit({
        padding: 20,
        minScale: 0.5,
        maxScale: 3,
        viewportArea: {
            width: 1391,
            height: 586,
            x: 0,
            y: 0
        }
    });
}