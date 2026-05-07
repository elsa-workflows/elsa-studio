import { createContext, useContext } from 'react';
import type { Edge } from '@xyflow/react';

// Lets a custom edge mutate the parent designer's edge list (delete edge,
// add/move/remove vertex). Exposed from the InnerDesigner that owns the
// edge state, consumed by ElsaEdge.
export interface EdgeOps {
    deleteEdge: (edgeId: string) => void;
    addVertex: (edgeId: string, position: { x: number; y: number }, segmentIndex?: number) => void;
    updateVertex: (edgeId: string, vertexIndex: number, position: { x: number; y: number }) => void;
    removeVertex: (edgeId: string, vertexIndex: number) => void;
    snapshotEdges: (next: Edge[]) => void;
}

export const EdgeOpsContext = createContext<EdgeOps | null>(null);

export function useEdgeOps(): EdgeOps {
    const ctx = useContext(EdgeOpsContext);
    if (!ctx) throw new Error('EdgeOpsContext is missing — ElsaEdge must render inside InnerDesigner');
    return ctx;
}
