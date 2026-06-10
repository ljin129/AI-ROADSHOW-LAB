const path = require('path')
const _timesTamp = new Date().getTime()
const isPro = process.env.NODE_ENV == 'pro'
const _resolve = (dir) => {
	return path.join(__dirname, dir)
}

const { name } = require('./package')
module.exports = {
	publicPath: `${isPro ? process.env.VUE_APP_CDN : ''}/Beta.AIRoadshowPC`,
	outputDir: 'dist/',
	lintOnSave: true,
	runtimeCompiler: false,
	// 生产环境是否生成 sourceMap 文件
	productionSourceMap: false,
	chainWebpack: (config) => {
		config.resolve.alias.set('@', _resolve('src')).set('_c', _resolve('src/components'))
		// 禁止打包为base64图片
		config.module
			.rule('images')
			.use('url-loader')
			.loader('url-loader')
			.tap((options) => Object.assign(options, { limit: 1 }))
	},
	configureWebpack: (config) => {
		let outputPro = {
			// 把子应用打包成 umd 库格式
			library: 'Beta.AIRoadshowPC',
			libraryTarget: 'umd',
			jsonpFunction: `webpackJsonp_${name}`,
			filename: `js/[name].${_timesTamp}.js`,
			chunkFilename: `js/[name].${_timesTamp}.js`,
		}
		config.output = Object.assign(config.output, outputPro)
		config.devtool = isPro ? false : 'source-map' // 取消主站webpack源码
	},
	css: {
		// 开启 CSS source maps?
		sourceMap: false,
		loaderOptions: {
			less: {
				javascriptEnabled: true,
			},
		},
	},
	devServer: {
		port: 9202,
		https: true,
		disableHostCheck: true,
		proxy: 'https://w-1.test.betawm.com/',
		headers: {
			'Access-Control-Allow-Origin': '*',
		},
	},
	pwa: {
		iconPaths: {
			favicon16: 'favicon.ico',
			favicon32: 'favicon.ico',
		},
	},
	parallel: true,
}
