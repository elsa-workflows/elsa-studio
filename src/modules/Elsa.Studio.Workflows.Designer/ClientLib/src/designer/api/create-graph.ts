import {Graph, Shape, Node} from '@antv/x6';
import {Selection} from "@antv/x6-plugin-selection";
import {Snapline} from "@antv/x6-plugin-snapline";
import {Transform} from "@antv/x6-plugin-transform";
import {Keyboard} from "@antv/x6-plugin-keyboard";
import {Clipboard} from '@antv/x6-plugin-clipboard'
import camelCase from 'lodash.camelcase';
import {DotNetComponentRef, graphBindings} from "./graph-bindings";
import {DotNetFlowchartDesigner} from "./dotnet-flowchart-designer";
import {Activity} from "../models";
import {updateActivity} from "./update-activity";

export async function createGraph(containerId: string, componentRef: DotNetComponentRef, readOnly: boolean): Promise<string> {
    const containerElement = document.getElementById(containerId);
    const interop = new DotNetFlowchartDesigner(componentRef);
    let silent = false;
    let lastSelectedNode: Node.Properties = null;

    const graph = new Graph({
        container: containerElement,
        autoResize: true,
        embedding: {
            // enabled: true,
            // findParent(arg: any) {
            //     const sourceNode = arg.node as Node.Properties;
            //     const sourceBox = sourceNode.getBBox();
            //     return this.getNodes().filter((targetNode) => {
            //         const targetBBox = targetNode.getBBox()
            //
            //         // Does the source activity node intersect with the target activity node?
            //         if (!sourceBox.isIntersectWithRect(targetBBox))
            //             return false;
            //
            //         const targetNodeId = targetNode.id;
            //         const targetActivityElementId = `activity-${targetNodeId}`;
            //         const targetNodeElement = document.getElementById(targetActivityElementId);
            //
            //         // Does the target activity element exist?
            //         if (!targetNodeElement)
            //             return false;
            //
            //         // Does the target activity contains embedded ports?
            //         const embeddedPortElements = targetNodeElement.querySelectorAll('.embedded-port');
            //
            //         if (embeddedPortElements.length == 0)
            //             return false;
            //
            //         // Check which of the embedded ports intersect with the source activity node.
            //         for (let i = 0; i < embeddedPortElements.length; i++) {
            //             const embeddedPortElement = embeddedPortElements[i];
            //             const embeddedPortElementRect = embeddedPortElement.getBoundingClientRect();
            //             const embeddedPortElementBBox = graph.pageToLocal(embeddedPortElementRect);
            //
            //             if (!sourceBox.isIntersectWithRect(embeddedPortElementBBox))
            //                 continue;
            //
            //             const embeddedPortName = embeddedPortElement.getAttribute('data-port-name');
            //             sourceNode.setProp(`embeddedPortName`, embeddedPortName);
            //             return true;
            //         }
            //
            //         return false
            //     })
            // },
        },
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
        interacting: {
            nodeMovable: () => !readOnly,
            arrowheadMovable: () => !readOnly,
            edgeMovable: () => !readOnly,
            vertexMovable: () => !readOnly,
            vertexAddable: () => !readOnly,
            vertexDeletable: () => !readOnly,
            edgeLabelMovable: () => !readOnly,
            magnetConnectable: () => !readOnly,
            toolsAddable: () => !readOnly,
            useEdgeTools: () => !readOnly,
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
            },
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
            embedding: {
                name: 'stroke',
                args: {
                    padding: -1,
                    attrs: {
                        stroke: '#73d13d',
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
            multiple: !readOnly,
            rubberEdge: false,
            rubberNode: true,
            rubberband: true,
            movable: !readOnly,
            showNodeSelectionBox: true
        }),
    );

    if (!readOnly) {
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

        graph.use(
            new Transform({
                resizing: {
                    enabled: true,

                }
            })
        );

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
        });

        // Paste the cells in the clipboard onto the graph.
        graph.bindKey(['ctrl+v', 'meta+v'], () => {
            if (!graph.isClipboardEmpty()) {
                const cells = graph.paste({offset: 32})
                graph.cleanSelection()
                graph.select(cells)
            }
            return false
        });
    }

    graph.on('blank:click', async () => {
        if(!!lastSelectedNode) {
            lastSelectedNode.setProp('selected-port', null);
        }
        await interop.raiseCanvasSelected();
    });

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

    graph.on('node:click', async args => {
        const {e, node} = args;
        const activity: Activity = node.data;
        const activityId = activity.id;
        const activityElementId = `activity-${activityId}`;
        const activityElement = document.getElementById(activityElementId);
        const embeddedPortElements = activityElement.querySelectorAll('.embedded-port');
        const mousePosition = graph.clientToLocal(e.clientX, e.clientY);

        // Check which of the embedded ports intersect with the selected node.
        for (let i = 0; i < embeddedPortElements.length; i++) {
            const embeddedPortElement = embeddedPortElements[i];
            const embeddedPortElementRect = embeddedPortElement.getBoundingClientRect();
            const embeddedPortElementBBox = graph.pageToLocal(embeddedPortElementRect);

            if (!embeddedPortElementBBox.containsPoint(mousePosition))
                continue;

            // Mark the node as unselected.
            if(graph.isSelected(node)) {
                graph.unselect(node);
            }

            const embeddedPortName = embeddedPortElement.getAttribute('data-port-name');
            node.setProp('selected-port', embeddedPortName);
            lastSelectedNode = node;
            
            await interop.raiseActivityEmbeddedPortSelected(activity, embeddedPortName);
            return;
        }

        if(!graph.isSelected(node)) {
            silent = true;
            graph.select(node);
            silent = false;
        }

        node.setProp('selected-port', null);
        await interop.raiseActivitySelected(activity);
    });

    graph.on('node:selected', async args => {
        const {node} = args;

        // if(!silent)
        //     graph.unselect(node);
    });

    graph.on('node:change:parent', e => {
        const node: Node.Properties = e.node;
        const parent = node.parent as any as Node.Properties;

        if (!parent)
            return;

        const childActivity: Activity = {...node.data};
        const parentActivity: Activity = {...parent.data};

        // Assign the child activity to the parent activity in the specified port property.
        const portName = node.getProp('embeddedPortName');
        const propName = camelCase(portName);
        parentActivity[propName] = childActivity;

        requestAnimationFrame(async () => {
            // Delete the node itself during the next animation frame. Doing it immediately doesn't cause X6 to update the graph.
            node.remove({deep: true});

            // Trigger a repaint of the parent node.
            debugger;
            parent.setData(parentActivity, {overwrite: true});
        });
    });

    const onGraphUpdated = async (e: any) => {
        await interop.raiseGraphUpdated();
    };

    const onNodeRemoved = async (e: any) => {
        const activity = e.node.data as Activity;
        await onGraphUpdated(e);
    };

    const onNodeAdded = async (e: any) => {
        const node = e.node as any;

        if (!node.isClone) {
            const activity = {...node.getData()} as Activity;
        }

        await onGraphUpdated(e);
    };

    graph.on('node:moved', onGraphUpdated);
    graph.on('node:added', onNodeAdded);
    graph.on('node:removed', onNodeRemoved);
    graph.on('edge:removed', onGraphUpdated);
    graph.on('edge:connected', onGraphUpdated);

    // Register the graph.
    const graphId = containerId;

    graphBindings[graphId] = {
        graphId: graphId,
        graph: graph,
        interop: interop
    };

    return graphId;
}