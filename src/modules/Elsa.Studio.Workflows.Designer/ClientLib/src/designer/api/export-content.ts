import { graphBindings } from "./graph-bindings";

export interface CaptureOptions {
    fileName?: string;
    padding?: number;
    format?: string;
}

export function exportContentToJPEG(graphId: string, options: CaptureOptions) {
    const { graph } = graphBindings[graphId];
    if (!graph) return;

    graph.exportJPEG(options.fileName + ".jpeg", {
        padding: options.padding,
    });
}
export function exportContentToPNG(graphId: string, options: CaptureOptions) {
    const { graph } = graphBindings[graphId];
    if (!graph) return;

    graph.exportPNG(options.fileName + ".png", {
        padding: options.padding,
    });
}
export function exportContentToSVG(graphId: string, options: CaptureOptions) {
    const { graph } = graphBindings[graphId];
    if (!graph) return;

    graph.exportSVG(options.fileName + ".svg", {});
}