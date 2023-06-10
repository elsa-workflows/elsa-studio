import {calculateSize} from "./calculate-size";

export async function loadData() {

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