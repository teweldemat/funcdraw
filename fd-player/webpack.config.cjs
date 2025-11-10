const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');

const publicPath = process.env.PUBLIC_PATH || '/';

module.exports = {
  entry: path.resolve(__dirname, 'src/main.tsx'),
  output: {
    filename: 'assets/[name].[contenthash].js',
    path: path.resolve(__dirname, 'dist'),
    publicPath,
    clean: true
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.js'],
    alias: {
      react$: require.resolve('react'),
      'react-dom$': require.resolve('react-dom'),
      'react/jsx-runtime$': require.resolve('react/jsx-runtime'),
      'react/jsx-dev-runtime$': require.resolve('react/jsx-dev-runtime')
    }
  },
  module: {
    rules: [
      {
        test: /\.tsx?$/,
        use: {
          loader: 'ts-loader',
          options: {
            transpileOnly: false,
            compilerOptions: {
              noEmit: false
            }
          }
        },
        exclude: /node_modules/
      },
      {
        test: /\.fs$/i,
        type: 'asset/source'
      },
      {
        test: /\.js$/i,
        include: path.resolve(__dirname, 'src/examples'),
        type: 'asset/source'
      },
      {
        test: /\.css$/i,
        use: ['style-loader', 'css-loader']
      }
    ]
  },
  plugins: [
    new HtmlWebpackPlugin({
      template: path.resolve(__dirname, 'index.html'),
      favicon: path.resolve(__dirname, 'favicon.svg')
    })
  ],
  devtool: 'source-map',
  devServer: {
    historyApiFallback: true,
    hot: true,
    port: 5184
  }
};
