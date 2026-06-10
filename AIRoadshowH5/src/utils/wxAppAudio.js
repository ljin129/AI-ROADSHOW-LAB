const nowTime = new Date().valueOf();
function printLog() {
    console.log(`wxAppAudio [${new Date().valueOf() - nowTime}] ${Array.prototype.slice.apply(arguments).join(' ')}`);
}
export class WXAppAudio {
	constructor(recordId, recordCb = null, sampleRate = '00') {
		console.log('WXAppAudio', recordId)
		this.sampleRate = sampleRate // 默认8k左边一位指供应商(0腾讯1火山2Deepgram) 右边一位指多少采样率(0:8K,1:16K)
		this.recordId = recordId
		this.socket = null // websocket实例
		this.aliveTimer = null // 保活计时器
		this.recordCb = recordCb
		this.reconnectTimer = null // 重启连接计时器
		this.hideReconnectTimer = null // 页面推到后台后重连
		this.initWebSocket()
		this.reInit = false // 标记重新链接
	}

	// 初始化webSocket
	initWebSocket(cb) {
		printLog('WebSocket 连接', this.recordId);
		this.socket = new WebSocket(`${process.env.VUE_APP_SOCKET}svc-aicoachplus-websocket/phone_call/pas/${this.recordId}:2`)
		// this.socket = new WebSocket(`ws://10.10.10.15:8090/svc-aicoachplus-websocket/phone_call/pas/${this.recordId}:2?lang=${bjsdk.query('lang') || 'zh-CN'}`)
		this.socket.onopen = (event) => {
			printLog('wxApp WebSocket 已连接', event);
			console.log('websocket', this.socket)
			this.socket.send(JSON.stringify({
action: 1,
action_data:{
				lang: bjsdk.query('lang') || 'zh-CN',
				sampleRate: this.sampleRate,
			}
}))
			if (cb && typeof cb === 'function') {
				cb()
			}
			this.keepAlive()
			this.reconnect()
		};
		this.socket.onmessage = (event) => {
			console.log('接到语音数据啦~~~', event.data)
			this.onMessage(event.data)
		}
		// 被关闭的处理
		this.socket.onclose = () => {
			printLog('websocket 被关闭')
			this.cancelAlive()
			if (this.reInit) {
				this.reInit = false
				this.initWebSocket()
			}
		}
		// 处理 WebSocket 错误
		this.socket.onerror = (error) => {
			console.error('wxApp WebSocket 错误:', error);
			console.error('wxApp WebSocket 错误:', error);
			console.error('当前 WebSocket 状态:', this.socket.readyState);
		
			// 打印 WebSocket 的 URL 信息
			console.error('WebSocket URL:', this.socket.url);
		
			// 打印时间戳
			console.error('错误发生时间:', new Date().toISOString());
		
			// 打印 WebSocket 错误码和原因
			if (error instanceof CloseEvent) {
				console.error('关闭代码:', error.code);
				console.error('关闭原因:', error.reason);
			}
		
			// 打印出错时的网络状态
			if (navigator.onLine !== undefined) {
				console.error('当前网络状态:', navigator.onLine ? '在线' : '离线');
			}
			this.closeWXAppAudio()
		};
	}

	// 接收消息
	onMessage(message) {
		if (this.hideReconnectTimer) {
			clearTimeout(this.hideReconnectTimer)
			this.hideReconnectTimer = null
		}
		if (typeof this.recordCb === 'function') {
			this.recordCb(message)
		}
	}

	// 发送websocket消息
	sendMessage(data) {
		printLog('wxAppAudio send', JSON.stringify(data), this.recordId)
		if (this.socket) {
			printLog('this.socket.readyState', this.socket.readyState, WebSocket.OPEN)
		}
		if (!this.socket || (this.socket && this.socket.readyState !== WebSocket.OPEN)) {
			this.closeWXAppAudio()
			printLog('this.socket 准备重连', this.recordId)
			this.initWebSocket(this.sendMessage.bind(this, data))
			
        } else {
			if (data.action && data.action === 30) {
				const content = JSON.parse(data.action_data.content || '{}')
				if (content && content.type === '30_1') {
					this.hideReconnectTimer = setTimeout(() => {
						this.reInit = true
						this.closeWXAppAudio()
						clearTimeout(this.hideReconnectTimer)
						this.hideReconnectTimer = null
					}, 1500)
				}
			}
			this.socket.send(JSON.stringify(data))
		}
	}

	// 保活机制
	keepAlive() {
		this.aliveTimer = setInterval(() => {
			if (this.socket) {
				console.log(`保活${this.recordId}:2`)
				this.socket.send(JSON.stringify({action:0}))
			} else {
				this.closeWXAppAudio()
				this.initWebSocket()
			}
		}, 10000)
	}

	// 取消保活
	cancelAlive() {
		if (this.aliveTimer) {
            clearInterval(this.aliveTimer)
            this.aliveTimer = null
        }
	}

	// 断开链接
	closeWXAppAudio() {
		printLog('关闭wxAppAudio socket')
		this.cancelAlive()
		if (this.reconnectTimer) {
			clearInterval(this.reconnectTimer)
			this.reconnectTimer = null
		}
		if (this.socket) {
			this.socket.close()
			this.socket = null
		}
	}

	// 重连机制，为了解决微信小程序退到后台，等几分钟后，再切到前台，此时websocket未连接，等用户操作时再连已经来不及了
	reconnect() {
		if (this.reconnectTimer) {
			return
		}
		this.reconnectTimer = setInterval(() => {
			if (!this.socket || (this.socket && this.socket.readyState !== WebSocket.OPEN)) {
				this.initWebSocket()
			}
		}, 1000)
	}
}
