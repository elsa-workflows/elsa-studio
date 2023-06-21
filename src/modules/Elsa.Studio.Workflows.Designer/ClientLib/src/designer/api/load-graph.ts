import {Model, Graph} from '@antv/x6';
import {graphBindings} from "./graph-bindings";

export function loadGraph(graphId: string, json: Model.FromJSONData) {
    const {graph} = graphBindings[graphId];
    
    graph.fromJSON(json);
    graph.centerContent({padding: 20});
}