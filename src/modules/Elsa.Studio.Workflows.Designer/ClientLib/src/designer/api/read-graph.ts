import {Graph, Cell} from '@antv/x6';
import {graphs} from "../internal/graphs";

export function readGraph(graphId: string): {
    cells: Cell.Properties[];
} {
    const graph: Graph = graphs[graphId];
    return graph.toJSON();
}