import { createRouter, createWebHistory } from 'vue-router'

import { dataCollect } from 'beta-v3/lib/util/index'

// 自动注入路由，页面无需引入
let routes = [{
	path: '/',
	redirect: '/home',
}]

const files = require.context('./modules/', false, /\.js$/)
files.keys().forEach((fileName) => {
	if (fileName.includes('menu.js')) return
	routes = routes.concat(files(fileName).default)
})
const router = createRouter({
	history: createWebHistory((/(\/prp\/[^/]+)/i.test(location.pathname) ? RegExp.$1 : '') + '/Beta.AIRoadshowH5'),
	routes,
	// 新增
	scrollBehavior(to, from, savePosition) {
		if (savePosition) {
			//解决页面从列表页跳转到详情页返回,初始在原来位置
			return savePosition
		} else {
			//解决页面跳转后页面高度和前一个页面高度一样
			return { x: 0, y: 0 }
		}
	},
})
// router.beforeEach对路由进行遍历，设置title
router.beforeEach((to, from, next) => {
	//判断是否有标题
	document.title = to.meta?.title || '';
	window.bjsdk?.spaEnd();

	next()
})
router.afterEach((to, from) => {
	console.log('路由切换', to.meta.isCollect);

	if (to.meta.isCollect === false) {
		console.log("不执行数据采集");
		window.bjsdk.dOn(
			'on-dc-ready',
			(isShare) => {
				dataCollect({}, isShare)
			},
			() => { },
			false,
			3,
			false,
			1
		);
	} else {
		dataCollect()//是否执行dc采集，默认为执行，如需关闭，请配置路由meta参数：isCollect: false
	}
})

export default router
