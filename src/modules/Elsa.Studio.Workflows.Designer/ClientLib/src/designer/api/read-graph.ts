import {Cell} from '@antv/x6';
import {graphBindings} from "./graph-bindings";

export function readGraph(graphId: string): {
    cells: Cell.Properties[];
} {
    const {graph} = graphBindings[graphId];
    return graph.toJSON();
}