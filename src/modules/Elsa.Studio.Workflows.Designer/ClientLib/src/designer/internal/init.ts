import {Graph, Shape} from '@antv/x6';
import {createActivityElement} from "./create-activity-element";
import {Activity} from "../models";

export function initialize() {

    Shape.HTML.register({
        shape: "elsa-activity",
        effect: ["data", "activityStats"],
        html(cell) {
            const activity: Activity = cell.getData();
            const selectedPort = cell.prop('selected-port');
            const activityStats = cell.prop('activityStats');
            return createActivityElement(activity, false, selectedPort, activityStats);
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

}