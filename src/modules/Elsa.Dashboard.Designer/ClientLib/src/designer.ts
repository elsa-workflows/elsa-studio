import {Graph, Shape} from '@antv/x6';
import {Snapline} from "@antv/x6-plugin-snapline";
import {Selection} from "@antv/x6-plugin-selection";
import {Transform} from "@antv/x6-plugin-transform";
import "../css/designer.css";

const activityTagName = "elsa-activity-wrapper";

debugger;

export async function createGraph(containerId: string) {
    const containerElement = document.getElementById(containerId);

    debugger;

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
        width: 2000,
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

    const data = await loadData()
    graph.fromJSON(data);

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

    // graph.use(
    //     new Scroller({
    //         enabled: true,
    //         modifiers: ['ctrl', 'meta'],
    //         autoResize: true,
    //         pannable: true
    //     }));

    graph.centerContent({padding: 20});
    graph.grid.update({color: '#f1f1f1'});
    //graph.zoomToFit({padding: 60});

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
}

function createActivityElement(activity): HTMLElement {
    console.debug("custom-html");
    const activityElement = document.createElement(activityTagName);
    activityElement.setAttribute("label", activity.label);
    return activityElement;
}

Shape.HTML.register({
    shape: "elsa-activity",
    effect: ["data"],
    html(cell) {
        const activity = cell.getData();
        return createActivityElement(activity);
    },
    ports: {
        groups: {
            in: {
                position: "left",
                attrs: {
                    circle: {
                        r: 5,
                        magnet: true,
                        stroke: "#0ea5e9",
                        strokeWidth: 2,
                        fill: "#fff",
                    },
                    text: {
                        fontSize: 12,
                        fill: "#888",
                    },
                },
                label: {
                    position: {
                        name: "outside",
                    },
                },
            },
            out: {
                position: "right",
                attrs: {
                    circle: {
                        r: 5,
                        magnet: true,
                        stroke: "#fff",
                        strokeWidth: 2,
                        fill: "#0ea5e9",
                    },
                    text: {
                        fontSize: 12,
                        fill: "#888",
                    },
                },
                label: {
                    position: {
                        name: "outside",
                    },
                },
            },
        },
    }
});

function calculateSize(): Promise<any> {
    const wrapper = document.createElement('div');
    const dummyActivity = {label: 'Write line'};
    const dummyActivityElement = createActivityElement(dummyActivity);
    wrapper.style.position = 'absolute';
    wrapper.appendChild(dummyActivityElement);

    // Append the temporary element to the DOM.
    const bodyElement = document.getElementsByTagName('body')[0];
    bodyElement.append(wrapper);

    // Wait for activity element to be completely rendered.
    // When using custom elements, they are rendered after they are mounted. Before then, they have a 0 width and height.
    return new Promise((resolve, reject) => {
        const checkSize = () => {
            const activityElement: Element = wrapper.getElementsByTagName(activityTagName)[0];
            const activityElementRect = activityElement.getBoundingClientRect();

            // If the custom element has no width or height yet, it means it has not yet rendered.
            if (activityElementRect.width == 0 || activityElementRect.height == 0) {
                // Request an animation frame and call ourselves back immediately after.
                window.requestAnimationFrame(checkSize);
            } else {
                const rect = wrapper.firstElementChild.getBoundingClientRect();
                const width = rect.width;
                const height = rect.height;

                // Remove the temporary element (used only to calculate its size).
                wrapper.remove();

                // Update size of the activity node and resolve the promise.
                resolve({width, height});
            }
        };

        // Begin try to get our element size.
        checkSize();
    });
}

Graph.registerEdge(
    'elsa-edge',
    {
        inherit: 'edge',
        attrs: {
            line: {
                stroke: '#C2C8D5',
                strokeWidth: 1,
                targetMarker: 'classic',
                size: 6,
            },
        },
    },
    true,
);

async function loadData() {

    const size = await calculateSize();

    return {
        nodes: [
            {
                id: 'node1', // String，可选，节点的唯一标识
                shape: 'elsa-activity',
                x: 40,       // Number，必选，节点位置的 x 值
                y: 40,       // Number，必选，节点位置的 y 值
                width: size.width,
                height: size.height,
                data: {
                    label: 'Write line', // String，节点标签
                },
                ports: [{
                    id: "port1",
                    group: "in",
                }, {
                    id: "port2",
                    group: "out",
                }]
            },
            {
                id: 'node2', // String，节点的唯一标识
                shape: 'elsa-activity',
                x: 320,      // Number，必选，节点位置的 x 值
                y: 40,      // Number，必选，节点位置的 y 值
                width: size.width,
                height: size.height,
                data: {
                    label: 'Write line', // String，节点标签
                },
                ports: [{
                    id: "port3",
                    group: "in",
                }, {
                    id: "port4",
                    group: "out",
                }]
            }
        ],
        edges: [
            {
                source: {
                    cell: 'node1',
                    port: 'port2',
                },
                target: {
                    cell: 'node2',
                    port: 'port3',
                },
                shape: 'elsa-edge'
            }
        ],
    };
}