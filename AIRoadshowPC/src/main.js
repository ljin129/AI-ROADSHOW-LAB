import Vue from 'vue'
import App from './App.vue'
import './public-path'
import routes from './router/index'
import VueRouter from 'vue-router'
import 'normalize.css' // 重置css
import ElementUI from 'element-ui'
import 'element-ui/lib/theme-chalk/index.css' // element-ui样式文件
import './common/filter.js'
Vue.config.productionTip = false


Vue.use(ElementUI, { size: "small", zIndex: 3000 });

Vue.use(VueRouter)

/* 
 * 接入乾坤
 */
let router = null
let instance = null
function render({ container, parentStore } = {}) {
	const pathName = location.pathname.includes("Beta.InsideManagePC")
		? "/Beta.InsideManagePC"
		: "/Beta.VipManagerV2PC";
	const _base = window.__POWERED_BY_QIANKUN__
		? `${pathName}/homeView/AIRoadshowPC`
		: (/(\/prp\/[^/]+)/i.test(location.pathname) ? RegExp.$1 : "") + "/Beta.AIRoadshowPC"
	router = new VueRouter({
		base: _base,
		mode: "history",
		routes,
	});

	instance = new Vue({
		router,
		data() {
			return {
				parentStore
			}
		},
		render: h => h(App)
	}).$mount(container ? container.querySelector('#app-AIRoadshowPC') : '#app-AIRoadshowPC')

}

if (!window.__POWERED_BY_QIANKUN__) {
	// 全局变量来判断环境
	render()
}

/**
 * bootstrap 只会在微应用初始化的时候调用一次，下次微应用重新进入时会直接调用 mount 钩子，不会再重复触发 bootstrap。
 * 通常我们可以在这里做一些全局变量的初始化，比如不会在 unmount 阶段被销毁的应用级别的缓存等。
 */
export async function bootstrap() {
	console.log('现在进入子应用Beta.SalesPadPC的bootstraped阶段')

}

/**
 * 应用每次进入都会调用 mount 方法，通常我们在这里触发应用的渲染方法
 */
export async function mount(props) {
	if (props.parentStore) {
		await props.parentStore.dispatch('getResource', { name: 'B应用的资源' })
	}
	console.log('现在进入子应用Beta.SalesPadPC的mount周期', props)

	render(props)
}

/**
 * 应用每次 切出/卸载 会调用的方法，通常在这里我们会卸载微应用的应用实例
 */
export async function unmount() {
	console.log('现在进入子应用Beta.SalesPadPC的unmount阶段')
	instance.$destroy()
	instance.$el.innerHTML = ''
	instance = null
	router = null
}

/**
 * 可选生命周期钩子，仅使用 loadMicroApp 方式加载微应用时生效
 */
export async function update(props) {
	console.log('update props', props)
}
