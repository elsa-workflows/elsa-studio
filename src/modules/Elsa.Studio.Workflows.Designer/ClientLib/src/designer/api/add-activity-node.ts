import {Node} from "@antv/x6";
import {graphs} from "../internal/graphs";

export async function addActivityNode(graphId: string, node: Node.Properties) {
    // Get graph reference.
    const graph = graphs[graphId];

    // Convert the node coordinates from page to local.
    const {x, y} = graph.pageToLocal(node.position);

    node.position = {x, y};
    node.size = {width: 200, height: 50};

    debugger;
    
    // Add the node to the graph.
    graph.addNode(node);
}