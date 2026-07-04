import {Edge, Graph, Node} from '@antv/x6';
import {GraphBinding} from './graph-bindings';

export type SequenceLayoutOrientation = 'vertical' | 'horizontal';

const SequenceGap = 160;

export function normalizeSequenceOrientation(value?: string | null): SequenceLayoutOrientation {
    return value === 'horizontal' ? 'horizontal' : 'vertical';
}

export function isHorizontalSequence(binding: GraphBinding): boolean {
    return normalizeSequenceOrientation(binding.layoutOrientation) === 'horizontal';
}

export function withSuppressedGraphUpdated<T>(binding: GraphBinding, action: () => T): T {
    binding.suppressGraphUpdated = (binding.suppressGraphUpdated || 0) + 1;
    try {
        return action();
    } finally {
        binding.suppressGraphUpdated = Math.max(0, (binding.suppressGraphUpdated || 0) - 1);
    }
}

export function arrangeSequenceGraph(binding: GraphBinding, orderedNodes?: Node<Node.Properties>[]) {
    const graph = binding.graph;
    const nodes = orderedNodes ?? sortSequenceNodes(graph.getNodes(), binding);

    withSuppressedGraphUpdated(binding, () => {
        nodes.forEach((node, index) => {
            node.setPosition(
                isHorizontalSequence(binding)
                    ? {x: index * SequenceGap, y: 0}
                    : {x: 0, y: index * SequenceGap},
                {silent: true},
            );
        });

        graph.getEdges().forEach(edge => graph.removeCell(edge, {silent: true}));
        buildSequenceEdges(graph, nodes).forEach(edge => graph.addEdge(edge, {silent: true}));
    });
}

export function setSequenceOrientation(binding: GraphBinding, orientation: string) {
    binding.layoutOrientation = normalizeSequenceOrientation(orientation);
    arrangeSequenceGraph(binding);
}

export function moveSelectedSequenceNode(binding: GraphBinding, direction: number): boolean {
    const delta = direction < 0 ? -1 : 1;
    const ordered = sortSequenceNodes(binding.graph.getNodes(), binding);
    const selectedIndex = ordered.findIndex(node => binding.graph.isSelected(node));
    if (selectedIndex < 0) return false;

    const targetIndex = selectedIndex + delta;
    if (targetIndex < 0 || targetIndex >= ordered.length) return false;

    const next = [...ordered];
    [next[selectedIndex], next[targetIndex]] = [next[targetIndex], next[selectedIndex]];
    arrangeSequenceGraph(binding, next);
    binding.graph.cleanSelection();
    binding.graph.select(next[targetIndex]);
    return true;
}

export function sortSequenceNodes(nodes: Node<Node.Properties>[], binding: GraphBinding): Node<Node.Properties>[] {
    const horizontal = isHorizontalSequence(binding);
    return [...nodes].sort((a, b) => {
        const aPosition = a.getPosition();
        const bPosition = b.getPosition();
        const primary = horizontal ? aPosition.x - bPosition.x : aPosition.y - bPosition.y;
        if (primary !== 0) return primary;
        return horizontal ? aPosition.y - bPosition.y : aPosition.x - bPosition.x;
    });
}

function buildSequenceEdges(graph: Graph, nodes: Node<Node.Properties>[]): Edge<Edge.Properties>[] {
    return nodes.slice(0, -1).map((node, index) => {
        const next = nodes[index + 1];
        return graph.createEdge({
            id: `${node.id}:sequence:${next.id}`,
            shape: 'elsa-sequence-edge',
            source: {cell: node.id},
            target: {cell: next.id},
            zIndex: -1,
        });
    });
}
