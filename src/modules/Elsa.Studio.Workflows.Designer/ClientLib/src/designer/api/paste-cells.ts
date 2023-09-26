import {Cell, Node, Edge} from "@antv/x6";
import {graphBindings} from "./graph-bindings";

export async function pasteCells(graphId: string, nodeProps: Node.Properties[], edgeProps: Edge.Properties[]) {
    // Get graph reference.
    const {graph} = graphBindings[graphId];

    // Create nodes and edges from node props and edge props.
    const nodes = nodeProps.map(x => graph.createNode(x));
    const edges = edgeProps.map(x => graph.createEdge(x));

    // Add the nodes and edges to the graph.
    graph.addCell(nodes);
    graph.addCell(edges);

    // Wait for the new cells to be rendered.
    requestAnimationFrame(() => {
        graph.cleanSelection();
        graph.select([...nodes, ...edges]);
    });
}