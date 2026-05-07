import { reactBindings } from '../bindings';
import type { ElsaActivityNode } from '../types';

// Called by .NET after it regenerates IDs server-side (HandlePasteCellsRequested).
// Mirrors the X6 path's pasteCells.
export function pasteReactCells(graphId: string, activityNodes: ElsaActivityNode[], edges: any[]): void {
    reactBindings[graphId]?.pasteCells(activityNodes, edges);
}
