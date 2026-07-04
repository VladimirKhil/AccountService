const path = require('path');

module.exports = {
  mode: 'production',
  entry: './src/AccountServiceClient.ts',
  output: {
    filename: 'AccountServiceClient.js',
    path: path.resolve(__dirname, 'dist'),
    library: {
      type: 'commonjs2',
    },
  },
  resolve: {
    extensions: ['.ts', '.js'],
  },
  module: {
    rules: [
      {
        test: /\.ts$/,
        use: 'ts-loader',
        exclude: /node_modules/,
      },
    ],
  },
};
