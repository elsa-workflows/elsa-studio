import {Graph} from '@antv/x6';
import {Snapline} from "@antv/x6-plugin-snapline";

const data = {
    nodes: [
        {
            id: 'node1', // String，可选，节点的唯一标识
            x: 40,       // Number，必选，节点位置的 x 值
            y: 40,       // Number，必选，节点位置的 y 值
            width: 80,   // Number，可选，节点大小的 width 值
            height: 40,  // Number，可选，节点大小的 height 值
            label: 'hello', // String，节点标签
        },
        {
            id: 'node2', // String，节点的唯一标识
            x: 160,      // Number，必选，节点位置的 x 值
            y: 180,      // Number，必选，节点位置的 y 值
            width: 80,   // Number，可选，节点大小的 width 值
            height: 40,  // Number，可选，节点大小的 height 值
            label: 'world!', // String，节点标签
        },
    ],
    edges: [
        {
            source: 'node1', // String，必须，起始节点 id
            target: 'node2', // String，必须，目标节点 id
        },
    ],
};


export function createGraph() {
    const graph = new Graph({
        container: document.getElementById('container'),
        autoResize: true,
        grid: {
            visible: true,
            size: 10,
        },
        background: {
            color: '#f5f5f5',
        },
        height: 1000,
        width: 2000,
        panning: {
            enabled: true,
        },
        mousewheel: {
            enabled: true,
            factor: 1.05,
            modifiers: ['ctrl', 'meta'],
        }
    });

    graph.fromJSON(data)
    graph.use(new Snapline({enabled: true}))
}