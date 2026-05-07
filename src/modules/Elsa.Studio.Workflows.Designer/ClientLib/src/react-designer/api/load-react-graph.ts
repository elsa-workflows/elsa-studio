import { reactBindings } from '../bindings';
import type { ElsaGraph } from '../types';

export function loadReactGraph(graphId: string, data: string | ElsaGraph): void {
    const binding = reactBindings[graphId];
    if (!binding) return;
    const graph = typeof data === 'string' ? (JSON.parse(data) as ElsaGraph) : data;
    binding.setGraph(graph);
}
