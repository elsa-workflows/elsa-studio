import {Cell, Edge} from '@antv/x6';
import {graphBindings} from "./graph-bindings";

export function readGraph(graphId: string): {
    cells: Cell.Properties[];
} {
    const {graph} = graphBindings[graphId];
    const model = graph.toJSON();
    
    // Filter out edges that don't have both a star and end node.
    model.cells = model.cells.filter((cell: Cell.Properties) => {
        
        if(cell.shape == 'elsa-activity')
            return true;
        
        if(cell.shape == 'elsa-edge')
        {
            const edge = cell as any;
            
            if(!!edge.source?.cell && !!edge.target?.cell)
                return true;
        }
        
        return false;
    });
    
    debugger;
    return model;
}