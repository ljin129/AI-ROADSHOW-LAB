const path = require('path')
const _timesTamp = new Date().getTime()
const isPro = process.env.NODE_ENV == 'pro'
const _resolve = (dir) => {
	return path.join(__dirname, dir)
}

module.exports = {
	publicPath: `${isPro ? process.env.VUE_APP_CDN : '/'}Beta.AIRoadshowH5`,
	outputDir: 'dist/',
	lintOnSave: true,
	runtimeCompiler: false,
	// 生产环境是否生成 sourceMap 文件
	productionSourceMap: false,
	chainWebpack: (config) => {
		config.resolve.alias.set('@', _resolve('src')).set('_c', _resolve('src/components'))
		// 禁止打包为base64图片
		config.module.rule('images').use('url-loader').loader('url-loader').tap((options) => Object.assign(options, { limit: 1 }));
	},
	configureWebpack: (config) => {
		const outputPro = {
			// 输出重构  打包编译后的 文件名称  【模块名称.时间戳】
			filename: `js/[name].${_timesTamp}.js`,
			chunkFilename: `js/[name].${_timesTamp}.js`,
		}
		config.output = Object.assign(config.output, outputPro)
		config.devtool = isPro ? false : 'source-map' // 取消主站webpack源码
		config.module.rules.push({
			test: /\.mjs$/,
			include: /node_modules/,
			type: 'javascript/auto',
		});
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
		port: 8199,
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
