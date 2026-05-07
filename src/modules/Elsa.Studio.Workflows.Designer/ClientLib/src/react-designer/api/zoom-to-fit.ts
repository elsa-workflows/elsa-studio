import { reactBindings } from '../bindings';

export function zoomReactGraphToFit(graphId: string): void {
    reactBindings[graphId]?.fitView();
}

export function centerReactGraphContent(graphId: string): void {
    reactBindings[graphId]?.centerContent();
}
