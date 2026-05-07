import { reactBindings } from '../bindings';
import type { ElsaActivityStats } from '../types';

export function updateReactActivityStats(graphId: string, activityId: string, stats: ElsaActivityStats): void {
    reactBindings[graphId]?.updateNodeStats(activityId, stats);
}
