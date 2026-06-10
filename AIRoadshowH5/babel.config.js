
module.exports = {
	presets: ['@vue/cli-plugin-babel/preset'],
	overrides: [
		{
			include: './node_modules/beta-v3',
			sourceType: 'unambiguous',
		}
	],
	plugins: [
		[
			'import',
			{
				libraryName: 'vant',
				libraryDirectory: 'es',
				style: true,
			},
		],
		[
			'import',
			{
				libraryName: 'beta-v3', //组件库名称
				camel2DashComponentName: false, //关闭驼峰自动转链式
				camel2UnderlineComponentName: false, //关闭蛇形自动转链式
				style: (name) => {
					return `${name.replace('/lib/', '/lib/style/')}.css`
				},
			},
			'beta-v3',
		],
		['@babel/plugin-proposal-optional-chaining'],
	],
}
