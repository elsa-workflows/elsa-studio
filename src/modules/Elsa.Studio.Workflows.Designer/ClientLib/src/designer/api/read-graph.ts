import {Cell} from '@antv/x6';
import {graphBindings} from "./graph-bindings";

export function readGraph(graphId: string): {
    cells: Cell.Properties[];
} {
    const {graph} = graphBindings[graphId];
    const model = graph.toJSON();
    
    // Filter out edges that don't have both a star and end node.
    model.cells = model.cells.filter(x => x.type !== 'edge' || (x as any).source && (x as any).target);
    
    return model;
}