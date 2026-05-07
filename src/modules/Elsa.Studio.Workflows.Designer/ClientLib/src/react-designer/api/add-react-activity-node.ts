import { reactBindings } from '../bindings';
import type { ElsaActivityNode } from '../types';

export function addReactActivityNode(graphId: string, node: ElsaActivityNode): void {
    const binding = reactBindings[graphId];
    if (!binding) return;

    // The wrapper supplies the drop point in page-coordinate space via
    // node.position (see FlowchartDesignerWrapper.AddNewActivityAsync).
    // Translate that into flow coordinates inside the designer.
    const dropPagePosition = node.position
        ? { x: node.position.x, y: node.position.y }
        : undefined;

    binding.addNode(node, dropPagePosition);
}
