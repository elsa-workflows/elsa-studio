const path = require('path');

module.exports = {
    entry: {
        prompt: './src/prompt.ts',
        designer: './src/designer.ts',
    },
    output: {
        filename: '[name].entry.js',
        path: path.resolve(__dirname, '..', 'wwwroot'),
        library: {
            type: "module",
        },
        module: true, // Enable outputting ES6 modules.
        environment: {
            module: true, // The environment supports ES6 modules.
        },
    },
    experiments: {
        outputModule: true, // This enables the experimental support for outputting ES6 modules
    },
    devtool: 'source-map',
    mode: 'development',
    module: {
        rules: [
            {
                test: /\.css$/,
                use: ['style-loader', 'css-loader'],
            },
            {
                test: /\.(eot|woff(2)?|ttf|otf|svg)$/i,
                type: 'asset'
            },
        ]
    }
};