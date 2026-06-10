import { CreatePlayer } from '@/utils/SAPlayer'

const DEFAULT_LANG = 'zh-CN'
const SPLIT_CHARS = new Set(['，', '。', ',', ':', '?', '!', '！', '？', '；', ';', '.'])

const createAudioContext = () => {
	const audioContext = new (window.AudioContext || window.webkitAudioContext)()
	if (audioContext && typeof audioContext.resume === 'function') {
		audioContext.resume().catch(() => {})
	}
	return audioContext
}

const sleep = (ms) => new Promise((resolve) => {
	window.setTimeout(resolve, ms)
})

const isArrayBuffer = (value) => Object.prototype.toString.call(value) === '[object ArrayBuffer]'

const getLang = () => {
	const bjsdk = window.bjsdk || {}
	if (typeof bjsdk.query === 'function') {
		return bjsdk.query('lang') || DEFAULT_LANG
	}
	return DEFAULT_LANG
}

const hasEnoughSpeakableText = (text) => {
	const source = String(text || '')
	if (!source.trim()) return false

	const chineseChars = source.match(/[\u4e00-\u9fa5]/g) || []
	const englishWords = source.match(/\b[a-zA-Z]+\b/g) || []
	const numbers = source.match(/[\d]+(?:\.\d+)?/g) || []
	const totalParts = chineseChars.length + englishWords.length + numbers.length

	return chineseChars.length > 10 || totalParts > 10
}

export class TtsPlayer {
	constructor({
		recordId = '',
		sampleRate = '00',
		customerId = '',
		onStateChange = null,
	} = {}) {
		this.recordId = recordId
		this.sampleRate = sampleRate || '00'
		this.customerId = customerId
		this.onStateChange = onStateChange
		this.socket = null
		this.player = null
		this.socketReady = false
		this.isRestarting = false
		this.isClosing = false
		this.aliveTimer = null
		this.pendingRetry = null
		this.monitorTimer = null
		this.currentKey = ''
		this.streamKey = ''
		this.audioText = ''
		this.streamQueue = []
		this.lastQueuedIndex = 0
		this.lastSentIndex = 0
		this.endTts = false
		this.isTextEnded = false
		this.hasAppendedStreamEnd = false
		this.isPlaying = false
		this.autoPlay = false
		this.pendingChunkId = ''
		this.currentCustomerId = customerId || ''

		if (this.recordId) {
			this.init(null, this.recordId)
		}
	}

	isSupported() {
		return typeof window !== 'undefined' &&
			typeof window.WebSocket !== 'undefined' &&
			(typeof window.AudioContext !== 'undefined' || typeof window.webkitAudioContext !== 'undefined')
	}

	emitState(key = this.currentKey, isPlaying = this.isPlaying) {
		if (typeof this.onStateChange === 'function') {
			this.onStateChange({
				key: key || '',
				isPlaying: !!isPlaying,
				supported: this.isSupported(),
			})
		}
	}

	createPlayerIfNeeded() {
		if (this.player) return
		this.player = CreatePlayer({
			ctxCreator: createAudioContext,
			mode: 1,
			onEnded: this.handleEnded.bind(this),
		})
	}

	init(onReady = null, recordId = '') {
		if (recordId) {
			this.recordId = String(recordId).trim()
		}
		if (!this.isSupported() || !this.recordId) return
		if (this.socket && (this.socket.readyState === WebSocket.OPEN || this.socket.readyState === WebSocket.CONNECTING)) {
			if (typeof onReady === 'function') onReady()
			return
		}

		this.createPlayerIfNeeded()
		this.isClosing = false
		this.socketReady = false
		const socketUrl = `${process.env.VUE_APP_SOCKET}svc-aicoachplus-websocket/phone_call/${this.recordId}?lang=${encodeURIComponent(getLang())}`
		this.socket = new WebSocket(socketUrl)
		this.socket.binaryType = 'arraybuffer'

		this.socket.onopen = () => {
			this.socketReady = true
			this.isRestarting = false
			this.sendRaw({
				action: 1,
				action_data: {
					lang: getLang(),
					sampleRate: this.sampleRate,
				},
			})
			this.keepAlive()
			if (typeof onReady === 'function') onReady()
			if (typeof this.pendingRetry === 'function') {
				const retry = this.pendingRetry
				this.pendingRetry = null
				retry()
			}
		}

		this.socket.onmessage = (event) => {
			this.clearMonitor()
			if (!this.currentKey) return

			if (isArrayBuffer(event.data)) {
				if (!this.player || !this.pendingChunkId) return
				if (!this.pendingChunkId.startsWith(`${this.currentKey}_`)) return
				this.player.append(event.data, true, this.currentKey)
				window.setTimeout(() => {
					this.isPlaying = true
					this.emitState(this.currentKey, true)
				}, 60)
				return
			}

			let payload = null
			try {
				payload = JSON.parse(event.data || '{}')
			} catch (error) {
				return
			}

			const { action, action_data = {} } = payload
			if (action === 13) {
				if (this.pendingChunkId && this.pendingChunkId === action_data.id) {
					this.endTts = true
					this.pendingChunkId = ''
					this.flushQueuedChunks()
					this.flushIfFinished()
				}
				return
			}

			if (action === 11 || action === 40) {
				this.stop()
			}
		}

		this.socket.onclose = () => {
			this.socket = null
			this.socketReady = false
			this.clearAlive()
			if (!this.isClosing) {
				this.isRestarting = false
			}
		}

		this.socket.onerror = () => {
			this.socketReady = false
		}
	}

	restartSocket(onReady = null, recordId = '') {
		if (recordId) {
			this.recordId = String(recordId).trim()
		}
		this.pendingChunkId = ''
		this.clearMonitor()
		this.clearAlive()
		const currentSocket = this.socket
		if (currentSocket) {
			this.isClosing = true
			currentSocket.onopen = null
			currentSocket.onmessage = null
			currentSocket.onclose = null
			currentSocket.onerror = null
			this.socket = null
			try {
				currentSocket.close()
			} catch (error) {}
		}
		this.socketReady = false
		this.isClosing = false
		this.isRestarting = true
		this.init(onReady, this.recordId)
	}

	sendRaw(payload) {
		if (!this.socket || this.socket.readyState !== WebSocket.OPEN) return
		this.socket.send(JSON.stringify(payload))
	}

	keepAlive() {
		this.clearAlive()
		this.aliveTimer = window.setInterval(() => {
			if (!this.socket || this.socket.readyState !== WebSocket.OPEN) return
			this.socket.send(JSON.stringify({ action: 0 }))
		}, 10000)
	}

	clearAlive() {
		if (this.aliveTimer) {
			clearInterval(this.aliveTimer)
			this.aliveTimer = null
		}
	}

	clearMonitor() {
		if (this.monitorTimer) {
			clearTimeout(this.monitorTimer)
			this.monitorTimer = null
		}
	}

	startMonitor() {
		this.clearMonitor()
		this.monitorTimer = window.setTimeout(() => {
			this.stop()
			this.init()
		}, 5000)
	}

	resetTrack(key, autoPlay, customerId = '') {
		this.currentKey = key
		this.streamKey = key
		this.audioText = ''
		this.streamQueue = []
		this.lastQueuedIndex = 0
		this.lastSentIndex = 0
		this.endTts = false
		this.isTextEnded = false
		this.hasAppendedStreamEnd = false
		this.autoPlay = !!autoPlay
		this.isPlaying = false
		this.pendingChunkId = ''
		this.currentCustomerId = String(customerId || this.customerId || '').trim()
		if (this.socket && this.socket.readyState === WebSocket.OPEN) {
			this.socket.send(JSON.stringify({ action: 2 }))
		}
		if (this.player) {
			this.player.stop()
		}
		this.emitState(key, false)
	}

	flushIfFinished() {
		if (!this.player) return
		if (this.endTts && this.isTextEnded && !this.pendingChunkId && this.streamQueue.length <= 0 && !this.hasAppendedStreamEnd) {
			this.hasAppendedStreamEnd = true
			this.player.append(null, false, this.currentKey)
		}
	}

	queueStreamChunk(chunk, chunkIndex, customerId = '') {
		if (!chunk) return
		this.streamQueue.push({
			chunk,
			chunkIndex,
			customerId,
		})
	}

	collectStreamChunks(isEnd) {
		if (isEnd) {
			const finalChunk = this.audioText.slice(this.lastQueuedIndex)
			if (finalChunk) {
				this.queueStreamChunk(finalChunk, this.audioText.length, this.currentCustomerId)
				this.lastQueuedIndex = this.audioText.length
			}
			return
		}

		let index = this.lastQueuedIndex
		while (index < this.audioText.length) {
			const currentChar = this.audioText[index]
			if (SPLIT_CHARS.has(currentChar)) {
				const chunk = this.audioText.slice(this.lastQueuedIndex, index + 1)
				if (hasEnoughSpeakableText(chunk)) {
					this.queueStreamChunk(chunk, index + 1, this.currentCustomerId)
					this.lastQueuedIndex = index + 1
				}
			}
			index += 1
		}
	}

	flushQueuedChunks() {
		if (this.pendingChunkId || !this.streamQueue.length) return
		const nextChunk = this.streamQueue.shift()
		if (!nextChunk) return
		this.sendChunk(nextChunk.chunk, nextChunk.chunkIndex, nextChunk.customerId)
	}

	async send(text = '', key = '', { channelId = '', customerId = '', isStream = false, isEnd = false, isClick = false, isRetry = false } = {}) {
		if (!this.isSupported()) return false
		const content = String(text || '').trim()
		if (!content || !key) return false
		const nextChannelId = String(channelId || key || '').trim()
		const nextCustomerId = String(customerId || this.customerId || '').trim()
		if (!nextChannelId) return false

		const shouldReplayFromStart = !!isClick
		const canUseCachedAudio = !!(this.player && (!isStream || isClick) && this.player.hasAudio(key, true))

		if (canUseCachedAudio && (key !== this.currentKey || shouldReplayFromStart || !isStream)) {
			this.resetTrack(key, isStream && !isClick, nextCustomerId)
			this.pendingRetry = null
			this.player.play(key)
			this.isPlaying = true
			this.emitState(key, true)
			return true
		}

		if (this.recordId !== nextChannelId) {
			this.pendingRetry = () => {
				this.send(content, key, { channelId: nextChannelId, customerId: nextCustomerId, isStream, isEnd, isClick, isRetry: true })
			}
			this.restartSocket(null, nextChannelId)
			return false
		}

		if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
			this.pendingRetry = () => {
				this.send(content, key, { channelId: nextChannelId, customerId: nextCustomerId, isStream, isEnd, isClick, isRetry: true })
			}
			if (!this.isRestarting) {
				this.isRestarting = true
				this.init(null, nextChannelId)
			}
			return false
		}

		if (key !== this.currentKey || shouldReplayFromStart) {
			this.resetTrack(key, isStream && !isClick, nextCustomerId)
			if (shouldReplayFromStart && !isRetry) {
				this.pendingRetry = () => {
					this.send(content, key, { channelId: nextChannelId, customerId: nextCustomerId, isStream, isEnd, isClick, isRetry: true })
				}
				this.restartSocket(null, nextChannelId)
				return false
			}
			await sleep(160)
		} else if (nextCustomerId) {
			this.currentCustomerId = nextCustomerId
		}

		this.audioText = content
		if (isEnd) {
			this.isTextEnded = true
		}

		if (!isStream || isClick) {
			this.streamQueue = []
			this.lastQueuedIndex = content.length
			this.lastSentIndex = content.length
			this.sendChunk(content, content.length, nextCustomerId)
			return true
		}

		this.collectStreamChunks(isEnd)
		this.flushQueuedChunks()

		if (isEnd) {
			this.flushIfFinished()
		}

		return true
	}

	sendChunk(chunk, chunkIndex, customerId = '') {
		if (!this.socket || this.socket.readyState !== WebSocket.OPEN) return
		this.lastSentIndex = chunkIndex
		this.endTts = false
		this.pendingChunkId = `${this.currentKey}_${chunkIndex}`
		this.startMonitor()
		this.socket.send(JSON.stringify({
			action: 10,
			action_data: {
				text: String(chunk || '').replace(/？/g, '?').replace(/！/g, '!'),
				customer_id: String(customerId || this.currentCustomerId || this.customerId || '').trim(),
				id: this.pendingChunkId,
			},
		}))
	}

	stop() {
		this.clearMonitor()
		this.audioText = ''
		this.streamQueue = []
		this.lastQueuedIndex = 0
		this.lastSentIndex = 0
		this.endTts = false
		this.isTextEnded = false
		this.hasAppendedStreamEnd = false
		this.autoPlay = false
		this.isPlaying = false
		this.pendingChunkId = ''
		if (this.socket && this.socket.readyState === WebSocket.OPEN) {
			this.socket.send(JSON.stringify({ action: 2 }))
		}
		if (this.player) {
			this.player.stop()
		}
		const previousKey = this.currentKey
		this.currentKey = ''
		this.streamKey = ''
		this.emitState(previousKey, false)
	}

	handleEnded(target) {
		if (target !== this.currentKey && target !== this.streamKey) return
		this.isPlaying = false
		this.emitState(target, false)
		if (!this.endTts || !this.isTextEnded) return
		this.currentKey = ''
		this.streamKey = ''
	}

	close() {
		this.isClosing = true
		this.pendingRetry = null
		this.clearMonitor()
		this.clearAlive()
		this.stop()
		if (this.socket) {
			this.socket.close()
			this.socket = null
		}
		if (this.player) {
			this.player.close(false)
			this.player = null
		}
	}
}
