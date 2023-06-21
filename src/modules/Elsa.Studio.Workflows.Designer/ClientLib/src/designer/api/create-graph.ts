import {Graph, Shape,} from '@antv/x6';
import {Selection} from "@antv/x6-plugin-selection";
import {Snapline} from "@antv/x6-plugin-snapline";
import {Transform} from "@antv/x6-plugin-transform";
import {Keyboard} from "@antv/x6-plugin-keyboard";
import {Clipboard} from '@antv/x6-plugin-clipboard'
import {DotNetComponentRef, graphBindings} from "./graph-bindings";
import {DotNetFlowchartDesigner} from "./dotnet-flowchart-designer";
import {Activity} from "../models";

export async function createGraph(containerId: string, componentRef: DotNetComponentRef): Promise<string> {
    const containerElement = document.getElementById(containerId);
    const interop = new DotNetFlowchartDesigner(componentRef);

    const graph = new Graph({
        container: containerElement,
        autoResize: true,

        grid: {
            type: 'mesh',
            visible: true,
            size: 20,
            args: {
                color: '#f1f1f1',
                thickness: 1,
            }
        },
        magnetThreshold: 0,
        height: 1000,
        width: 1000,
        panning: {
            enabled: true,
            modifiers: ['ctrl', 'meta'],
        },
        mousewheel: {
            enabled: true,
            factor: 1.05,
            modifiers: ['ctrl', 'meta'],
            minScale: 0.5,
            maxScale: 3,
        },
        connecting: {
            router: 'manhattan',
            connector: {
                name: 'rounded',
                args: {
                    radius: 8,
                },
            },
            anchor: 'center',
            connectionPoint: 'anchor',
            allowBlank: false,
            snap: {
                radius: 20,

            },
            createEdge() {
                return graph.createEdge({
                    shape: 'elsa-edge',
                    attrs: {
                        line: {
                            strokeDasharray: '5 5',
                        },
                    },
                    zIndex: -1,
                })
            },
            validateConnection({targetMagnet}) {
                return !!targetMagnet
            }
        },
        highlighting: {
            magnetAdsorbed: {
                name: 'stroke',
                args: {
                    attrs: {
                        fill: '#fff',
                        stroke: '#31d0c6',
                        strokeWidth: 4,
                    },
                },
            },
        }
    });

    graph.use(new Snapline({
        enabled: true,
        className: 'elsa-snapline',
    }));

    graph.use(
        new Selection({
            enabled: true,
            multiple: true,
            rubberEdge: false,
            rubberNode: true,
            rubberband: true,
            movable: true,
            showNodeSelectionBox: true
        }),
    );

    graph.use(
        new Keyboard({
            enabled: true
        })
    );

    graph.use(
        new Clipboard({
            enabled: true,
        }),
    )

    // We could enable this, but then we also need to update the Blazor activity component to support resizing.
    graph.use(
        new Transform({
            resizing: {
                enabled: true,

            }
        })
    );

    graph.on('blank:click', async () => await interop.raiseCanvasSelected());

    // Move the clicked node to the front. This helps when the user clicks on a node that is behind another node.
    graph.on('node:mousedown', ({node}) => {
        node.toFront();
    });

    // Change the edge's color and style when it is connected to a magnet.
    graph.on('edge:connected', ({edge}) => {
        edge.attr({
            line: {
                strokeDasharray: '',
            },
        })
    });

    graph.on("edge:mouseenter", ({cell}) => {
        cell.addTools([
            {name: "vertices"},
            {
                name: "button-remove",
                args: {distance: 20},
            },
        ]);
    });

    graph.on("edge:mouseleave", ({cell}) => {
        if (cell.hasTool("button-remove")) {
            cell.removeTool("button-remove");
        }
    });
    
    graph.on('node:selected', async e => {
        const node = e.node;
        const activity: Activity = node.data;
        await interop.raiseActivitySelected(activity);
    });

    // Delete node when pressing delete key on keyboard.
    graph.bindKey('del', () => {
        const cells = graph.getSelectedCells()
        if (cells.length) {
            graph.removeCells(cells)
        }
    });

    // Copy the cells in the graph to the internal clipboard with Ctrl+C.
    graph.bindKey(['ctrl+c', 'meta+c'], () => {
        const cells = graph.getSelectedCells()
        if (cells.length) {
            graph.copy(cells)
        }
        return false
    })

    // Paste the cells in the clipboard onto the graph.
    graph.bindKey(['ctrl+v', 'meta+v'], () => {
        if (!graph.isClipboardEmpty()) {
            const cells = graph.paste({offset: 32})
            graph.cleanSelection()
            graph.select(cells)
        }
        return false
    })

    // Register the graph.
    const graphId = containerId;
    
    graphBindings[graphId] = {
        graphId: graphId,
        graph: graph,
        interop: interop
    };

    return graphId;
}
