import { useEffect, useRef, memo } from 'react';
import { Handle, Position, useNodeId, type NodeProps } from '@xyflow/react';
import type { ElsaActivity, ElsaActivityStats, ElsaPort } from '../types';

export interface ActivityNodeData {
    activity: ElsaActivity;
    ports: ElsaPort[];
    activityStats?: ElsaActivityStats;
    selectedPort?: string | null;
    onEmbeddedPortClick?: (activity: ElsaActivity, portName: string) => void;
    [key: string]: unknown;
}

const activityTagName = 'elsa-activity-wrapper';

// The activity body itself is a Blazor custom element registered via
// RegisterForJavaScript("elsa-activity-wrapper"). We just create the tag
// and set the same attributes the X6 path sets in create-activity-element.ts;
// Blazor hydrates it on insertion.
function ActivityNodeImpl({ id, data }: NodeProps) {
    const hostRef = useRef<HTMLDivElement | null>(null);
    const elementRef = useRef<HTMLElement | null>(null);
    const nodeId = useNodeId();
    const { activity, ports, activityStats, selectedPort, onEmbeddedPortClick } = data as unknown as ActivityNodeData;
    const elementId = `activity-${id}`;

    useEffect(() => {
        const host = hostRef.current;
        if (!host) return;
        const el = document.createElement(activityTagName) as any;
        el.id = elementId;
        el.setAttribute('element-id', elementId);
        el.setAttribute('activity-id', id);
        el.setAttribute('activity-json', JSON.stringify(activity));
        if (selectedPort) el.setAttribute('selected-port-name', selectedPort);
        if (activityStats) el.stats = activityStats;
        host.appendChild(el);
        elementRef.current = el;
        return () => {
            if (elementRef.current && elementRef.current.parentNode === host) {
                host.removeChild(elementRef.current);
            }
            elementRef.current = null;
        };
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [id]);

    useEffect(() => {
        const el = elementRef.current as any;
        if (!el) return;
        // Avoid retriggering a Blazor custom-element re-hydration when the
        // activity reference changed but the JSON content didn't — otherwise
        // every parent re-render flickers the node and can swallow the first
        // click that prompted the re-render.
        const json = JSON.stringify(activity);
        if (el.getAttribute('activity-json') !== json) {
            el.setAttribute('activity-json', json);
        }
        const currentSelectedPort = el.getAttribute('selected-port-name');
        if (selectedPort) {
            if (currentSelectedPort !== selectedPort) el.setAttribute('selected-port-name', selectedPort);
        } else if (currentSelectedPort !== null) {
            el.removeAttribute('selected-port-name');
        }
        if (el.stats !== activityStats) el.stats = activityStats ?? null;
    }, [activity, selectedPort, activityStats]);

    // Delegated click listener for embedded ports rendered by ActivityWrapper
    // (V1/V2). When a user clicks the ".embedded-port" body inside a composite
    // activity (e.g. Sequence's Body), we navigate into it. The .embedded-port
    // div carries the port name in data-port-name. We stop propagation so
    // React Flow's onNodeClick doesn't also fire.
    useEffect(() => {
        const host = hostRef.current;
        if (!host) return;
        const handler = (event: MouseEvent) => {
            const target = event.target as HTMLElement | null;
            if (!target) return;
            const portEl = target.closest<HTMLElement>('.embedded-port');
            if (!portEl) return;
            const portName = portEl.getAttribute('data-port-name');
            if (!portName) return;
            event.stopPropagation();
            // eslint-disable-next-line no-console
            console.log('[react-designer] embedded-port click', {
                clickedActivityId: activity?.id,
                clickedActivityType: activity?.type,
                portName,
                hasOccupied: portEl.classList.contains('embedded-port-occupied'),
            });
            onEmbeddedPortClick?.(activity, portName);
        };
        host.addEventListener('click', handler);
        return () => host.removeEventListener('click', handler);
    }, [activity, onEmbeddedPortClick]);

    const inPorts = ports.filter(p => p.group === 'in');
    const outPorts = ports.filter(p => p.group === 'out');

    return (
        // Don't pin width/height — the React Flow node wrapper auto-sizes to
        // this container, which auto-sizes to the activity body's natural
        // size. That keeps the click target flush with the visible card.
        <div ref={hostRef} className="elsa-react-activity-host" data-node-id={nodeId}>
            {(inPorts.length > 0 ? inPorts : [{ id: 'in', group: 'in' } as ElsaPort]).map((p, i, arr) => (
                <Handle
                    key={`in-${p.id}`}
                    id={p.id}
                    type="target"
                    position={Position.Left}
                    style={{ top: `${((i + 1) / (arr.length + 1)) * 100}%` }}
                />
            ))}
            {(outPorts.length > 0 ? outPorts : [{ id: 'out', group: 'out' } as ElsaPort]).map((p, i, arr) => (
                <Handle
                    key={`out-${p.id}`}
                    id={p.id}
                    type="source"
                    position={Position.Right}
                    style={{ top: `${((i + 1) / (arr.length + 1)) * 100}%` }}
                />
            ))}
        </div>
    );
}

export const ActivityNode = memo(ActivityNodeImpl);
