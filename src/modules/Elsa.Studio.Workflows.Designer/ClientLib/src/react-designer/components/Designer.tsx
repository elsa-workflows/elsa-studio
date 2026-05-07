import { useCallback, useEffect, useImperativeHandle, useMemo, useRef, useState, forwardRef } from 'react';
import {
    ReactFlow,
    ReactFlowProvider,
    Background,
    BackgroundVariant,
    Controls,
    MiniMap,
    Panel,
    MarkerType,
    ConnectionLineType,
    applyNodeChanges,
    applyEdgeChanges,
    useReactFlow,
    type Node,
    type Edge,
    type NodeChange,
    type EdgeChange,
    type Connection,
    type DefaultEdgeOptions,
    type IsValidConnection,
    type NodeMouseHandler,
} from '@xyflow/react';
import '@xyflow/react/dist/style.css';

import type { ActivityDescriptorDto, ElsaActivityNode, ElsaActivityStats, ElsaEdge, ElsaGraph, ElsaActivity, ElsaPort } from '../types';
import { ActivityNode, type ActivityNodeData } from './ActivityNode';
import { DotNetReactDesigner } from '../dotnet-bridge';
import { dagreLayout } from '../internal/dagre-layout';
import { computeSnap, type SnapGuide } from '../internal/snap-lines';
import { SnapLines } from './SnapLines';
import { ElsaEdge as ElsaEdgeComponent, type ElsaEdgeData } from './ElsaEdge';
import { EdgeOpsContext, type EdgeOps } from './EdgeOpsContext';
import { ConnectMenu } from './ConnectMenu';

export interface DesignerHandle {
    setGraph: (graph: ElsaGraph) => void;
    setReadOnly: (readOnly: boolean) => void;
    selectNode: (id: string) => void;
    fitView: () => void;
    centerContent: () => void;
    readGraph: () => ElsaGraph;
    addNode: (node: ElsaActivityNode, dropPagePosition?: { x: number; y: number }) => void;
    updateNode: (node: ElsaActivityNode) => void;
    updateNodeStats: (activityId: string, stats: ElsaActivityStats) => void;
    autoLayout: () => void;
    pasteCells: (activityNodes: ElsaActivityNode[], edges: any[]) => void;
}

interface DesignerProps {
    initialReadOnly: boolean;
    interop: DotNetReactDesigner;
}

const nodeTypes = { 'elsa-activity': ActivityNode };
const edgeTypes = { 'elsa-edge': ElsaEdgeComponent };

const defaultEdgeOptions: DefaultEdgeOptions = {
    type: 'elsa-edge',
    animated: true,
    markerEnd: {
        type: MarkerType.ArrowClosed,
        width: 18,
        height: 18,
        color: '#94a3b8',
    },
    style: {
        stroke: '#94a3b8',
        strokeWidth: 2,
    },
};

const connectionLineStyle = {
    stroke: '#0ea5e9',
    strokeWidth: 2,
    strokeDasharray: '6 4',
};

function miniMapNodeColor(node: Node): string {
    const activity = (node.data as any)?.activity;
    if (activity?.canStartWorkflow === true) return '#0ea5e9';
    return '#cbd5e1';
}

function toReactFlowNode(
    node: ElsaActivityNode,
    onEmbeddedPortClick: (activity: ElsaActivity, portName: string) => void,
): Node<ActivityNodeData> {
    return {
        id: node.id,
        type: 'elsa-activity',
        position: { x: node.position?.x ?? 0, y: node.position?.y ?? 0 },
        data: {
            activity: node.data,
            ports: node.ports?.items ?? [],
            activityStats: node.activityStats,
            selectedPort: null,
            onEmbeddedPortClick,
        },
        // Intentionally no style.width/height: the React Flow wrapper
        // auto-sizes to the rendered activity body so there's no padding
        // gap between the visible card and the click target.
    };
}

function toReactFlowEdge(edge: ElsaEdge, index: number): Edge {
    const id = `${edge.source.cell}:${edge.source.port ?? ''}->${edge.target.cell}:${edge.target.port ?? ''}#${index}`;
    return {
        id,
        source: edge.source.cell,
        target: edge.target.cell,
        sourceHandle: edge.source.port,
        targetHandle: edge.target.port,
        data: { vertices: edge.vertices ?? [] } satisfies ElsaEdgeData,
    };
}

function fromReactFlowEdge(edge: Edge): ElsaEdge {
    const vertices = (edge.data as ElsaEdgeData | undefined)?.vertices ?? [];
    return {
        source: { cell: edge.source, port: edge.sourceHandle ?? '' },
        target: { cell: edge.target, port: edge.targetHandle ?? '' },
        shape: 'elsa-edge',
        vertices,
    };
}

function fromReactFlowNode(node: Node<ActivityNodeData>): ElsaActivityNode {
    // Prefer the measured size (the rendered body's actual dimensions) over a
    // stale persisted size; fall back through style and finally a sensible
    // default for the very first save before measurement runs.
    const measuredW = node.measured?.width;
    const measuredH = node.measured?.height;
    const styleW = typeof node.style?.width === 'number' ? node.style.width : undefined;
    const styleH = typeof node.style?.height === 'number' ? node.style.height : undefined;
    const data = node.data as unknown as ActivityNodeData;
    return {
        id: node.id,
        shape: 'elsa-activity',
        position: { x: node.position.x, y: node.position.y },
        size: { width: measuredW ?? styleW ?? 200, height: measuredH ?? styleH ?? 80 },
        data: data.activity,
        ports: { items: data.ports ?? [] },
        activityStats: data.activityStats,
    };
}

interface HistorySnapshot {
    nodes: Node<ActivityNodeData>[];
    edges: Edge[];
}

const HISTORY_LIMIT = 100;

const InnerDesigner = forwardRef<DesignerHandle, DesignerProps>(function InnerDesigner(props, ref) {
    const { initialReadOnly, interop } = props;
    const [nodes, setNodes] = useState<Node<ActivityNodeData>[]>([]);
    const [edges, setEdges] = useState<Edge[]>([]);
    const [readOnly, setReadOnly] = useState<boolean>(initialReadOnly);
    const [snapGuides, setSnapGuides] = useState<SnapGuide[]>([]);
    const flow = useReactFlow();
    const containerRef = useRef<HTMLDivElement | null>(null);
    // Tracks the structural fingerprint of the last graph we loaded; used to
    // decide whether to fitView. We always re-apply nodes/edges to keep state
    // canonical with C#, but only re-fit the camera when content shape changes
    // — otherwise canvas clicks (which sometimes trigger a same-content reload)
    // would jiggle the view on every interaction.
    const lastFitSignatureRef = useRef<string | null>(null);

    // Inline activity picker state. Set when the user drags from an output
    // handle and releases on empty canvas; cleared on pick/cancel.
    const [connectMenu, setConnectMenu] = useState<{
        sourceNodeId: string;
        sourceHandleId: string | null;
        clientX: number;
        clientY: number;
    } | null>(null);
    const [activityCatalog, setActivityCatalog] = useState<ActivityDescriptorDto[]>([]);
    const [catalogLoading, setCatalogLoading] = useState(false);
    const catalogLoadedRef = useRef(false);
    // Tracks the source of an in-progress connect drag (set by onConnectStart,
    // cleared on connect-end or cancel) so we know what to wire when the user
    // drops on empty canvas.
    const connectDragRef = useRef<{ nodeId: string; handleId: string | null } | null>(null);

    // Live refs for keyboard handlers (which see stale state otherwise).
    const nodesRef = useRef(nodes);
    const edgesRef = useRef(edges);
    nodesRef.current = nodes;
    edgesRef.current = edges;
    const readOnlyRef = useRef(readOnly);
    readOnlyRef.current = readOnly;

    // ----- History (undo/redo) -----------------------------------------------
    // We snapshot on committed changes only — drag end, add, remove, edge add,
    // activity update — never on every dragging tick.
    const historyRef = useRef<{ stack: HistorySnapshot[]; cursor: number; suspend: number }>({
        stack: [],
        cursor: -1,
        suspend: 0,
    });

    const snapshot = useCallback((nextNodes: Node<ActivityNodeData>[], nextEdges: Edge[]) => {
        const h = historyRef.current;
        if (h.suspend > 0) return;
        const snap: HistorySnapshot = {
            nodes: nextNodes.map(n => ({ ...n, data: { ...n.data } })),
            edges: nextEdges.map(e => ({ ...e })),
        };
        h.stack = h.stack.slice(0, h.cursor + 1);
        h.stack.push(snap);
        if (h.stack.length > HISTORY_LIMIT) h.stack.shift();
        h.cursor = h.stack.length - 1;
    }, []);

    const restore = useCallback((snap: HistorySnapshot) => {
        const h = historyRef.current;
        h.suspend++;
        try {
            setNodes(snap.nodes.map(n => ({ ...n, data: { ...n.data } })));
            setEdges(snap.edges.map(e => ({ ...e })));
        } finally {
            // Defer the suspend release so the resulting setState doesn't snapshot.
            queueMicrotask(() => { h.suspend--; });
        }
    }, []);

    const undo = useCallback(() => {
        const h = historyRef.current;
        if (h.cursor <= 0) return;
        h.cursor--;
        restore(h.stack[h.cursor]);
        interop.raiseGraphUpdated();
    }, [restore, interop]);

    const redo = useCallback(() => {
        const h = historyRef.current;
        if (h.cursor >= h.stack.length - 1) return;
        h.cursor++;
        restore(h.stack[h.cursor]);
        interop.raiseGraphUpdated();
    }, [restore, interop]);

    // ----- Embedded port click ----------------------------------------------
    const onEmbeddedPortClickRef = useRef<(activity: ElsaActivity, portName: string) => void>(() => {});
    onEmbeddedPortClickRef.current = useCallback((activity: ElsaActivity, portName: string) => {
        const activityId = activity?.id;
        setNodes(prev => prev.map(n => {
            if (n.id !== activityId) return n;
            return { ...n, data: { ...n.data, selectedPort: portName }, selected: false } as Node<ActivityNodeData>;
        }));
        interop.raiseActivityEmbeddedPortSelected(activity, portName);
    }, [interop]);
    const onEmbeddedPortClick = useCallback((activity: ElsaActivity, portName: string) => {
        onEmbeddedPortClickRef.current(activity, portName);
    }, []);

    // ----- Imperative handle exposed to .NET via createReactGraph -----------
    useImperativeHandle(ref, () => ({
        setGraph: (graph: ElsaGraph) => {
            const newNodes = (graph.nodes ?? []).map(n => toReactFlowNode(n, onEmbeddedPortClick));
            const newEdges = (graph.edges ?? []).map(toReactFlowEdge);
            setNodes(newNodes);
            setEdges(newEdges);
            // Reset history on a fresh load.
            historyRef.current = { stack: [{ nodes: newNodes, edges: newEdges }], cursor: 0, suspend: 0 };
            // Only re-fit the camera if the graph shape actually changed.
            const signature = `${(graph.nodes ?? []).map(n => n.id).sort().join(',')}|${(graph.edges ?? [])
                .map(e => `${e.source?.cell ?? ''}:${e.source?.port ?? ''}>${e.target?.cell ?? ''}:${e.target?.port ?? ''}`)
                .sort().join(',')}`;
            if (signature !== lastFitSignatureRef.current) {
                lastFitSignatureRef.current = signature;
                requestAnimationFrame(() => flow.fitView({ padding: 0.2 }));
            }
        },
        setReadOnly: (value: boolean) => setReadOnly(value),
        selectNode: (id: string) => {
            setNodes(prev => prev.map(n => ({ ...n, selected: n.id === id })));
        },
        fitView: () => flow.fitView({ padding: 0.2 }),
        centerContent: () => flow.fitView({ padding: 0.2 }),
        readGraph: () => ({
            nodes: nodes.map(fromReactFlowNode),
            edges: edges.map(fromReactFlowEdge),
        }),
        addNode: (node, dropPagePosition) => {
            const reactNode = toReactFlowNode(node, onEmbeddedPortClick);
            if (dropPagePosition) {
                const clientX = dropPagePosition.x - window.scrollX;
                const clientY = dropPagePosition.y - window.scrollY;
                reactNode.position = flow.screenToFlowPosition({ x: clientX, y: clientY });
            }
            setNodes(prev => {
                const next = [
                    ...prev.map(n => ({ ...n, selected: false })),
                    { ...reactNode, selected: true },
                ];
                snapshot(next, edgesRef.current);
                return next;
            });
            interop.raiseGraphUpdated();
        },
        updateNode: (node) => {
            setNodes(prev => {
                const next = prev.map(n => {
                    if (n.id !== node.id) return n;
                    return {
                        ...n,
                        data: {
                            ...n.data,
                            activity: node.data,
                            ports: node.ports?.items ?? n.data.ports,
                            activityStats: node.activityStats ?? n.data.activityStats,
                        },
                        style: {
                            ...(n.style ?? {}),
                            width: node.size?.width ?? (n.style as any)?.width,
                            height: node.size?.height ?? (n.style as any)?.height,
                        },
                    };
                });
                snapshot(next, edgesRef.current);
                return next;
            });
            interop.raiseGraphUpdated();
        },
        updateNodeStats: (activityId, stats) => {
            // Stats updates are not user actions, don't snapshot.
            setNodes(prev => prev.map(n => {
                if (n.id !== activityId) return n;
                return { ...n, data: { ...n.data, activityStats: stats } };
            }));
        },
        autoLayout: () => {
            setNodes(prev => {
                const next = dagreLayout(prev, edgesRef.current);
                snapshot(next, edgesRef.current);
                requestAnimationFrame(() => flow.fitView({ padding: 0.2 }));
                return next;
            });
            interop.raiseGraphUpdated();
        },
        pasteCells: (activityNodes, edgeCells) => {
            const newNodes = activityNodes.map(n => toReactFlowNode(n, onEmbeddedPortClick));
            const newEdges: Edge[] = edgeCells.map((e: any, i: number) => ({
                id: `${e.source?.cell}:${e.source?.port ?? ''}->${e.target?.cell}:${e.target?.port ?? ''}#paste-${Date.now()}-${i}`,
                source: e.source?.cell,
                target: e.target?.cell,
                sourceHandle: e.source?.port,
                targetHandle: e.target?.port,
            }));
            setNodes(prev => {
                const next = [...prev.map(n => ({ ...n, selected: false })), ...newNodes.map(n => ({ ...n, selected: true }))];
                snapshot(next, [...edgesRef.current, ...newEdges]);
                return next;
            });
            setEdges(prev => [...prev, ...newEdges]);
            interop.raiseGraphUpdated();
        },
    }), [flow, nodes, edges, interop, onEmbeddedPortClick, snapshot]);

    // ----- Change handlers --------------------------------------------------
    const onNodesChange = useCallback((changes: NodeChange[]) => {
        if (readOnly) return;
        const positionDone = changes.some(c => c.type === 'position' && (c as any).dragging === false);
        const removed = changes.some(c => c.type === 'remove');
        setNodes(prev => {
            const next = applyNodeChanges(changes, prev) as Node<ActivityNodeData>[];
            if (positionDone || removed) snapshot(next, edgesRef.current);
            return next;
        });
        if (positionDone || removed) interop.raiseGraphUpdated();
    }, [readOnly, interop, snapshot]);

    const onEdgesChange = useCallback((changes: EdgeChange[]) => {
        if (readOnly) return;
        const removed = changes.some(c => c.type === 'remove');
        setEdges(prev => {
            const next = applyEdgeChanges(changes, prev);
            if (removed) snapshot(nodesRef.current, next);
            return next;
        });
        if (removed) interop.raiseGraphUpdated();
    }, [readOnly, interop, snapshot]);

    const onConnect = useCallback((connection: Connection) => {
        if (readOnly) return;
        if (!connection.source || !connection.target) return;
        const edge: Edge = {
            id: `${connection.source}:${connection.sourceHandle ?? ''}->${connection.target}:${connection.targetHandle ?? ''}#${Date.now()}`,
            source: connection.source,
            target: connection.target,
            sourceHandle: connection.sourceHandle ?? undefined,
            targetHandle: connection.targetHandle ?? undefined,
        };
        setEdges(prev => {
            const next = [...prev, edge];
            snapshot(nodesRef.current, next);
            return next;
        });
        interop.raiseGraphUpdated();
    }, [readOnly, interop, snapshot]);

    // Inline activity picker: when the user releases a connect-drag on empty
    // canvas (not on a target handle), open the picker at the release point.
    const onConnectStart = useCallback((_e: any, params: { nodeId: string | null; handleId: string | null; handleType: string | null }) => {
        if (readOnly) {
            connectDragRef.current = null;
            return;
        }
        if (params.nodeId && params.handleType === 'source') {
            connectDragRef.current = { nodeId: params.nodeId, handleId: params.handleId };
        } else {
            connectDragRef.current = null;
        }
    }, [readOnly]);

    const onConnectEnd = useCallback((event: MouseEvent | TouchEvent) => {
        const drag = connectDragRef.current;
        connectDragRef.current = null;
        if (readOnly || !drag) return;

        // If the release happened on a handle/node, React Flow's onConnect
        // already handled it — bail out.
        const target = event.target as HTMLElement | null;
        if (target?.closest('.react-flow__handle, .react-flow__node')) return;

        const clientX = (event as MouseEvent).clientX
            ?? (event as TouchEvent).changedTouches?.[0]?.clientX
            ?? 0;
        const clientY = (event as MouseEvent).clientY
            ?? (event as TouchEvent).changedTouches?.[0]?.clientY
            ?? 0;

        // Lazy-load the activity catalog the first time the user opens the menu.
        if (!catalogLoadedRef.current) {
            catalogLoadedRef.current = true;
            setCatalogLoading(true);
            (interop as DotNetReactDesigner).getAvailableActivities()
                .then(list => setActivityCatalog(list))
                .catch(() => setActivityCatalog([]))
                .finally(() => setCatalogLoading(false));
        }

        setConnectMenu({
            sourceNodeId: drag.nodeId,
            sourceHandleId: drag.handleId,
            clientX,
            clientY,
        });
    }, [readOnly, interop]);

    const closeConnectMenu = useCallback(() => setConnectMenu(null), []);

    const onConnectMenuPick = useCallback(async (descriptor: ActivityDescriptorDto) => {
        const menu = connectMenu;
        if (!menu) return;
        // Match the drag-drop pipeline: pass page-relative coords; binding.addNode
        // converts them to flow coords via screenToFlowPosition.
        const pageX = menu.clientX + window.scrollX;
        const pageY = menu.clientY + window.scrollY;
        setConnectMenu(null);
        const created = await (interop as DotNetReactDesigner)
            .addActivityAtPosition(descriptor.typeName, descriptor.version, pageX, pageY);
        const newId = created?.id;
        if (!newId) return;
        // Auto-connect from the original source port to the new node's "In".
        const edge: Edge = {
            id: `${menu.sourceNodeId}:${menu.sourceHandleId ?? ''}->${newId}:In#${Date.now()}`,
            source: menu.sourceNodeId,
            target: newId,
            sourceHandle: menu.sourceHandleId ?? undefined,
            targetHandle: 'In',
        };
        setEdges(prev => {
            const next = [...prev, edge];
            snapshot(nodesRef.current, next);
            return next;
        });
        interop.raiseGraphUpdated();
    }, [connectMenu, interop, snapshot]);

    const isValidConnection: IsValidConnection = useCallback((connection) => {
        const { source, target, sourceHandle, targetHandle } = connection as Connection;
        if (!source || !target || source === target) return false;
        const sourceNode = nodesRef.current.find(n => n.id === source);
        const targetNode = nodesRef.current.find(n => n.id === target);
        const sourcePorts: ElsaPort[] = (sourceNode?.data as any)?.ports ?? [];
        const targetPorts: ElsaPort[] = (targetNode?.data as any)?.ports ?? [];
        const isSourceOut = sourcePorts.some(p => p.id === sourceHandle && p.group === 'out')
            || sourcePorts.length === 0;
        const isTargetIn = targetPorts.some(p => p.id === targetHandle && p.group === 'in')
            || targetPorts.every(p => p.group !== 'in');
        return isSourceOut && isTargetIn;
    }, []);

    const onNodeClick: NodeMouseHandler = useCallback((_e, node) => {
        const activity = (node.data as unknown as ActivityNodeData).activity;
        interop.raiseActivitySelected(activity as ElsaActivity);
    }, [interop]);

    const onNodeDoubleClick: NodeMouseHandler = useCallback((_e, node) => {
        const activity = (node.data as unknown as ActivityNodeData).activity;
        interop.raiseActivityDoubleClick(activity as ElsaActivity);
    }, [interop]);

    const onPaneClick = useCallback(() => {
        interop.raiseCanvasSelected();
    }, [interop]);

    // Edge-level operations exposed via context to the custom ElsaEdge.
    const edgeOps = useMemo<EdgeOps>(() => ({
        deleteEdge: (edgeId) => {
            setEdges(prev => {
                const next = prev.filter(e => e.id !== edgeId);
                snapshot(nodesRef.current, next);
                return next;
            });
            interop.raiseGraphUpdated();
        },
        addVertex: (edgeId, position, segmentIndex) => {
            setEdges(prev => {
                const next = prev.map(e => {
                    if (e.id !== edgeId) return e;
                    const verts = ((e.data as ElsaEdgeData | undefined)?.vertices ?? []).slice();
                    const idx = segmentIndex ?? verts.length;
                    verts.splice(idx, 0, position);
                    return { ...e, data: { ...(e.data ?? {}), vertices: verts } };
                });
                snapshot(nodesRef.current, next);
                return next;
            });
            interop.raiseGraphUpdated();
        },
        updateVertex: (edgeId, vertexIndex, position) => {
            // No snapshot here — drag updates fire continuously. snapshotEdges()
            // is called explicitly on drag end via EdgeOps.snapshotEdges below.
            setEdges(prev => prev.map(e => {
                if (e.id !== edgeId) return e;
                const verts = ((e.data as ElsaEdgeData | undefined)?.vertices ?? []).slice();
                if (vertexIndex < 0 || vertexIndex >= verts.length) return e;
                verts[vertexIndex] = position;
                return { ...e, data: { ...(e.data ?? {}), vertices: verts } };
            }));
        },
        removeVertex: (edgeId, vertexIndex) => {
            setEdges(prev => {
                const next = prev.map(e => {
                    if (e.id !== edgeId) return e;
                    const verts = ((e.data as ElsaEdgeData | undefined)?.vertices ?? []).slice();
                    if (vertexIndex < 0 || vertexIndex >= verts.length) return e;
                    verts.splice(vertexIndex, 1);
                    return { ...e, data: { ...(e.data ?? {}), vertices: verts } };
                });
                snapshot(nodesRef.current, next);
                return next;
            });
            interop.raiseGraphUpdated();
        },
        snapshotEdges: (next) => {
            snapshot(nodesRef.current, next);
            interop.raiseGraphUpdated();
        },
    }), [snapshot, interop]);

    // Snap-to-other-node alignment guides while dragging. We compute against
    // the live state (after React Flow applied its own delta) and snap by
    // patching the node position. Guides are visible only mid-drag.
    const onNodeDrag = useCallback((_e: any, draggedNode: Node) => {
        if (readOnly) return;
        const { position, guides } = computeSnap(draggedNode, nodesRef.current);
        if (position.x !== draggedNode.position.x || position.y !== draggedNode.position.y) {
            setNodes(prev => prev.map(n => (n.id === draggedNode.id ? { ...n, position } : n)));
        }
        setSnapGuides(guides);
    }, [readOnly]);

    const onNodeDragStart = useCallback(() => {
        if (readOnly) return;
        setSnapGuides([]);
    }, [readOnly]);

    const onNodeDragStop = useCallback(() => {
        setSnapGuides([]);
    }, []);

    // ----- Keyboard: copy / paste / undo / redo -----------------------------
    // React Flow already handles Delete/Backspace via deleteKeyCode; we layer
    // on Ctrl/Cmd shortcuts. Listener is scoped to the container so global
    // hotkeys elsewhere on the page aren't hijacked.
    const clipboardRef = useRef<{ nodes: Node<ActivityNodeData>[]; edges: Edge[] } | null>(null);

    useEffect(() => {
        const container = containerRef.current;
        if (!container) return;

        const onKeyDown = (e: KeyboardEvent) => {
            if (readOnlyRef.current) {
                // In read-only mode we still allow undo/redo only if there's history;
                // skip clipboard ops entirely.
            }
            const mod = e.metaKey || e.ctrlKey;
            if (!mod) return;

            const key = e.key.toLowerCase();

            // Undo: Ctrl/Cmd+Z (without shift).
            if (key === 'z' && !e.shiftKey) {
                e.preventDefault();
                undo();
                return;
            }
            // Redo: Ctrl/Cmd+Y, or Ctrl/Cmd+Shift+Z.
            if (key === 'y' || (key === 'z' && e.shiftKey)) {
                e.preventDefault();
                redo();
                return;
            }
            if (readOnlyRef.current) return;

            if (key === 'c') {
                const selectedNodes = nodesRef.current.filter(n => n.selected);
                if (selectedNodes.length === 0) return;
                const selectedIds = new Set(selectedNodes.map(n => n.id));
                const includedEdges = edgesRef.current.filter(eg => selectedIds.has(eg.source) && selectedIds.has(eg.target));
                clipboardRef.current = {
                    nodes: selectedNodes.map(n => ({ ...n, data: { ...n.data } })),
                    edges: includedEdges.map(eg => ({ ...eg })),
                };
                e.preventDefault();
                return;
            }
            if (key === 'x') {
                const selectedNodes = nodesRef.current.filter(n => n.selected);
                if (selectedNodes.length === 0) return;
                const selectedIds = new Set(selectedNodes.map(n => n.id));
                const includedEdges = edgesRef.current.filter(eg => selectedIds.has(eg.source) && selectedIds.has(eg.target));
                clipboardRef.current = {
                    nodes: selectedNodes.map(n => ({ ...n, data: { ...n.data } })),
                    edges: includedEdges.map(eg => ({ ...eg })),
                };
                setNodes(prev => {
                    const next = prev.filter(n => !selectedIds.has(n.id));
                    snapshot(next, edgesRef.current.filter(eg => !selectedIds.has(eg.source) && !selectedIds.has(eg.target)));
                    return next;
                });
                setEdges(prev => prev.filter(eg => !selectedIds.has(eg.source) && !selectedIds.has(eg.target)));
                interop.raiseGraphUpdated();
                e.preventDefault();
                return;
            }
            if (key === 'v') {
                const clip = clipboardRef.current;
                if (!clip || clip.nodes.length === 0) return;
                // Send to .NET so it generates fresh IDs and processes embedded ports;
                // .NET will then call back into pasteReactCells.
                const activityCells = clip.nodes.map(n => fromReactFlowNode(n));
                const edgeCells = clip.edges.map(eg => ({
                    shape: 'elsa-edge',
                    source: { cell: eg.source, port: eg.sourceHandle ?? '' },
                    target: { cell: eg.target, port: eg.targetHandle ?? '' },
                }));
                interop.raisePasteCellsRequested(activityCells, edgeCells);
                e.preventDefault();
                return;
            }
            if (key === 'a') {
                e.preventDefault();
                setNodes(prev => prev.map(n => ({ ...n, selected: true })));
                return;
            }
        };

        container.addEventListener('keydown', onKeyDown);
        return () => container.removeEventListener('keydown', onKeyDown);
    }, [undo, redo, snapshot, interop]);

    const proOptions = useMemo(() => ({ hideAttribution: true }), []);

    return (
        // tabIndex makes the container focusable so its keydown listener fires.
        <div ref={containerRef} tabIndex={-1} style={{ width: '100%', height: '100%', outline: 'none' }}>
            <EdgeOpsContext.Provider value={edgeOps}>
            <ReactFlow
                nodes={nodes}
                edges={edges}
                nodeTypes={nodeTypes}
                edgeTypes={edgeTypes}
                defaultEdgeOptions={defaultEdgeOptions}
                connectionLineType={ConnectionLineType.SmoothStep}
                connectionLineStyle={connectionLineStyle}
                isValidConnection={isValidConnection}
                onNodesChange={onNodesChange}
                onEdgesChange={onEdgesChange}
                onConnect={onConnect}
                onConnectStart={onConnectStart}
                onConnectEnd={onConnectEnd}
                onNodeClick={onNodeClick}
                onNodeDoubleClick={onNodeDoubleClick}
                onNodeDragStart={onNodeDragStart}
                onNodeDrag={onNodeDrag}
                onNodeDragStop={onNodeDragStop}
                onPaneClick={onPaneClick}
                nodesDraggable={!readOnly}
                nodesConnectable={!readOnly}
                elementsSelectable={true}
                deleteKeyCode={readOnly ? null : ['Delete', 'Backspace']}
                multiSelectionKeyCode={['Shift', 'Meta', 'Control']}
                proOptions={proOptions}
                fitView
            >
                <Background variant={BackgroundVariant.Dots} gap={20} size={1.4} color="#cbd5e1" />
                <Controls showInteractive={!readOnly} />
                <MiniMap pannable zoomable nodeColor={miniMapNodeColor} maskColor="rgba(15, 23, 42, 0.06)" />
                <SnapLines guides={snapGuides} />
                <Panel position="top-left" className="elsa-react-flow-badge">
                    React Flow
                </Panel>
            </ReactFlow>
            </EdgeOpsContext.Provider>
            {connectMenu && (
                <ConnectMenu
                    clientX={connectMenu.clientX}
                    clientY={connectMenu.clientY}
                    activities={activityCatalog}
                    loading={catalogLoading}
                    onPick={onConnectMenuPick}
                    onClose={closeConnectMenu}
                />
            )}
        </div>
    );
});

export const Designer = forwardRef<DesignerHandle, DesignerProps>(function Designer(props, ref) {
    return (
        <ReactFlowProvider>
            <InnerDesigner ref={ref} {...props} />
        </ReactFlowProvider>
    );
});
