import {Graph, Shape,} from '@antv/x6';
import {Selection} from "@antv/x6-plugin-selection";
import {Snapline} from "@antv/x6-plugin-snapline";
import {Transform} from "@antv/x6-plugin-transform";
import {v4 as uuid} from 'uuid';
import {graphs} from "../internal/graphs";

export async function createGraph(containerId: string): Promise<string> {
    const containerElement = document.getElementById(containerId);

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
            rubberEdge: true,
            rubberNode: true,
            rubberband: true,
            movable: true,
            showNodeSelectionBox: true
        }),
    );

    // We could enable this, but then we also need to update the Blazor activity component to support resizing.
    graph.use(
        new Transform({
            resizing: {
                enabled: false,

            },
            rotating: {
                enabled: false,
            }
        })
    );

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
    
    const graphId = containerId;
    graphs[graphId] = graph;

    return graphId;
}
