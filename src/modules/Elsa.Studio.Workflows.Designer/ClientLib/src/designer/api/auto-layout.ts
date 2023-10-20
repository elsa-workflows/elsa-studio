import { DagreLayout } from '@antv/layout';
import { loadGraph } from './load-graph';


const dagreLayout = new DagreLayout({
    type: 'dagre',
    rankdir: 'LR',
    align: 'DR',
    ranksep: 35,
    nodesep: 15,
})

export function autoLayout(graphId: string, model: any) {
    
    const newModel = dagreLayout.layout(model);

    loadGraph(graphId, newModel);
}