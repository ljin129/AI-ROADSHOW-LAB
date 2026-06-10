const files = require.context('./modules', false, /\.js$/) // 引入所有路由文件

// 遍历路由文件注入路由, 无需单独引入
let routes = []
files.keys().forEach((fileName) => {
	routes = routes.concat(files(fileName).default)
})

export default routes
