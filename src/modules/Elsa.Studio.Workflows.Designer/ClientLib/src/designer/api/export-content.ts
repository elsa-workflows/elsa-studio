import { graphBindings } from "./graph-bindings";

export function exportContentToJPEG(graphId: string, filename: string) {
    const { graph } = graphBindings[graphId];
    if (!graph) return;

    const container = document.getElementById(graphId);
    const width = container?.clientWidth ?? 1200;
    const height = container?.clientHeight ?? 800;

    graph.exportJPEG(filename + ".jpeg", { backgroundColor: '#fff', height: height, width:width });
}
export function exportContentToPNG(graphId: string, filename: string) {
    const { graph } = graphBindings[graphId];
    if (!graph) return;

    const container = document.getElementById(graphId);
    const width = container?.clientWidth ?? 1200;
    const height = container?.clientHeight ?? 800;

    graph.exportPNG(filename + ".png", { backgroundColor: '#fff', height: height, width: width });
}
export function exportContentSVG(graphId: string, filename: string) {
    const { graph } = graphBindings[graphId];
    if (!graph) return;

    const container = document.getElementById(graphId);
    const width = container?.clientWidth ?? 1200;
    const height = container?.clientHeight ?? 800;

    graph.exportSVG(filename + ".svg", { });
}