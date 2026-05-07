import { reactBindings } from '../bindings';
import type { ElsaActivityNode } from '../types';

export function updateReactActivity(graphId: string, node: ElsaActivityNode): void {
    reactBindings[graphId]?.updateNode(node);
}
