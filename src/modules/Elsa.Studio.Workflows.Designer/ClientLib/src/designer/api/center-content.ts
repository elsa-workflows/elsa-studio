import {Model, Graph} from '@antv/x6';
import {graphs} from "../internal/graphs";

export function centerContent(graphId: string) {
    const graph: Graph = graphs[graphId];
    graph.centerContent({
        padding: 20,
        useCellGeometry: true
    });
}