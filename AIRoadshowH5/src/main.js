import { createApp } from 'vue'
// import { updateOpenTag } from './utils/updataOpenData'
import bsdk from 'beta-jsdk'
import App from './App.vue'
import router from './router'
import 'normalize.css'
import '@/assets/css/reset.css'
import '@/assets/css/base.css'
import '@/assets/css/global.less'


// updateOpenTag();

const isActivityCombatPage = (pageUrl = '') => `${pageUrl || ''}`.includes('activityCombat')

const adaptActivityCombatPageUrl = (pageUrl = '') => {
	try {
		const url = new URL(pageUrl, window.location.origin)
		const practiceRecordId = url.searchParams.get('practiceRecordId') || url.searchParams.get('PracticeRecordId') || ''
		if (practiceRecordId && !url.searchParams.get('TestId')) {
			url.searchParams.set('TestId', practiceRecordId)
		}
		return url.toString()
	} catch (error) {
		return pageUrl
	}
}

bsdk.config({
	// 禁掉分享和授权，避免透传进来的bjwt被更改
	authOps: {
		binds: 1,
	},
	shareOps: {
		enable: 0,
	},
	navOps: {
		pass: ['fS', 'sTime', 'isP'],
		reqAbsPath: false, //将url转换为绝对路径
		//router是异步方法，需强制同步刷新页面
		async nav(ops, navOps) {
			//jsdk路由处理
			const _goPage = (_url, _rpc = false) => {
				if (_rpc) window.location.replace(_url)
				else window.location.href = _url
			}
			let _url = ops.url || '';//目标地址，非vue路由跳转地址，此地址会拼接上参数
			const rpc = navOps.replace || false;//是否replace方式
			const isMini = window.__wxjs_environment === 'miniprogram';//是否在微信小程序中
			if (isMini) {
				if (bjsdk.isNum(_url)) {
					//小程序返回
					wx.miniProgram.navigateBack({ delta: Math.abs(_url) });//支持0和正整数
					return;
				}
				const _base = router.options.history.base || '';//获取当前项目的基础路径
				if (!_url.includes(_base) && !bsdk.isBeta(_url)) _url = `${_base}/${_url}`;
				const _par = {};//小程序必要参数
				const pageUrl = window.bjsdk.url(_url, true, _par);
				const rpcAll = navOps.replaceAll || false;//是否关闭所有页面，打开到应用内的某个页面，仅小程序中使用
				if (pageUrl.includes('home')) {
					wx.miniProgram.reLaunch({ url: `/pages/index/index?pageUrl=${encodeURIComponent(pageUrl)}` })
				} else if (isActivityCombatPage(pageUrl)) {
					const adaptedPageUrl = adaptActivityCombatPageUrl(pageUrl)
					const activityCombatUrl = `/pagesA/testContent/index?pageUrl=${encodeURIComponent(adaptedPageUrl)}`
					if (rpcAll) {
						wx.miniProgram.reLaunch({ url: activityCombatUrl })
					} else if (rpc) {
						wx.miniProgram.redirectTo({ url: activityCombatUrl })
					} else {
						wx.miniProgram.navigateTo({ url: activityCombatUrl })
					}
				} else {
					const roadshowPageUrl = `/pages/roadshowTest/index?pageUrl=${encodeURIComponent(pageUrl)}`
					if (rpcAll) {
						wx.miniProgram.reLaunch({ url: roadshowPageUrl })
					} else if (rpc) {
						wx.miniProgram.redirectTo({ url: roadshowPageUrl })
					} else if (pageUrl.includes('identity')) {
						wx.miniProgram.navigateTo({ url: `/pages/index/index?pageUrl=${encodeURIComponent(pageUrl)}` })
					} else {
						wx.miniProgram.navigateTo({ url: roadshowPageUrl })
					}
				}
			} else if (bjsdk.isNum(_url)) {
				//非小程序返回
				window.history.go(_url);
				// router.go(_url)

			} else if (_url.indexOf('http') == 0 || bsdk.isBeta(_url) || navOps.isAbsPath) {
				_goPage(_url, rpc)
			} else {
				// app 跳转
				if (bsdk.isApp && !rpc) {
					// 若在app中且不是replace方式, 则需要判断参数中是否有_target=_blank,如果有则需要用location跳转打开新的webview
					const isNewView = bsdk.query('_target', _url) || bsdk.query('target', _url) || ''
					if (isNewView == '_blank') {
						const _base = router.options.history.base || '';//获取当前项目的基础路径
						_url = bsdk.url(`${_base}/${_url}`, true)
						_goPage(_url, false)
						return false
					}
				}
				// 企微跳转 或 app的replace方式跳转
				const p = ops.path || '';//vue路由跳转地址，不带参数
				const par = { ...ops.queryObj };//参数对象
				if (p.indexOf('/') == 0) {
					await router.push({ path: p, replace: rpc, query: par })
				} else {
					await router.push({ name: p, replace: rpc, query: par })
				}
			}
		},
	},
	wxOps: {
		debug: false,
		// agentApi: ['sendChatMessage'],
		// openTagList: ['wx-open-launch-weapp'],
		jsApiList: [
			'chooseImage',
			'startRecord', //开始录音
			'stopRecord', //停止录音接口
			'onVoiceRecordEnd', // 监听录音自动停止接口
			'playVoice', //播放语音接口
			'stopVoice', //停止播放接口
			'onVoicePlayEnd', //监听语音播放完毕接口
			'translateVoice', //语音转文字接口
			'uploadVoice', //上传语音接口
			'downloadVoice',
			"getLocation",
			'getKeyBoardHeight',
		], // 必填，随意一个接口即可
		agentApi: [
			'startRecord', //开始录音
			'stopRecord', //停止录音接口
			'onVoiceRecordEnd', // 监听录音自动停止接口
			'playVoice', //播放语音接口
			'stopVoice', //停止播放接口
			'onVoicePlayEnd', //监听语音播放完毕接口
			'translateVoice', //语音转文字接口
			'uploadVoice', //上传语音接口
			'downloadVoice',
			"getLocation"
		],
	},
	apiOps: {
		onClose() {
			const curRouteMeta = router.currentRoute.value.meta || {};
			console.log('curRouteMeta.onAppClose', router.currentRoute.value, location.pathname)
			if (curRouteMeta.onAppClose) {
				bsdk.dEmit('on-app-close', {})
			} else {
				console.log('close')
				bsdk.call('close')
			}
			return true
		},
	},
	// logOps: {
	//      //上报限制，默认10分钟内只能上报5条
	// 	 limit: 200,
	// },
})

createApp(App).use(router).mount('#app')
