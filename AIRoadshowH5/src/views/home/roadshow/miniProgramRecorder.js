const DEFAULT_ERROR_MESSAGE = '语音转文字失败，请重新录音'
const RECORD_ERROR_MESSAGE = '录音失败，请重新录音'

const getBjsdk = () => window.bjsdk || {}

const readLang = () => {
	const bjsdk = getBjsdk()
	if (typeof bjsdk.query === 'function') {
		return bjsdk.query('lang') || 'zh-CN'
	}
	return 'zh-CN'
}

const readToken = () => {
	const bjsdk = getBjsdk()
	if (typeof bjsdk.gLS === 'function') {
		return bjsdk.gLS('bjwt') || ''
	}
	if (typeof bjsdk.gJWT === 'function') {
		return bjsdk.gJWT() || ''
	}
	return ''
}

const toText = (value) => {
	if (value === null || value === undefined) return ''
	return `${value}`.trim()
}

export class MiniProgramRecorder {
	constructor({
		recordId = '',
		sampleRate = '00',
		onRecordingStart = null,
		onPartial = null,
		onResult = null,
		onError = null,
	} = {}) {
		this.recordId = recordId
		this.sampleRate = sampleRate || '00'
		this.onRecordingStart = onRecordingStart
		this.onPartial = onPartial
		this.onResult = onResult
		this.onError = onError
		this.socket = null
		this.pendingMessages = []
		this.isClosing = false
		this.bufferedText = ''
		this.bufferedFileId = ''
		this.resultHandled = false

		if (this.recordId) {
			this.connect()
		}
	}

	resetBufferedResult() {
		this.bufferedText = ''
		this.bufferedFileId = ''
		this.resultHandled = false
	}

	consumeBufferedResult() {
		if (this.resultHandled) {
			return {
				fileId: '',
				text: '',
			}
		}

		this.resultHandled = true
		const payload = {
			fileId: this.bufferedFileId,
			text: this.bufferedText,
		}
		this.bufferedText = ''
		this.bufferedFileId = ''
		return payload
	}

	emitBufferedResult(text = '', fileId = '') {
		if (this.resultHandled) return

		const nextText = toText(text) || this.bufferedText
		const nextFileId = toText(fileId) || this.bufferedFileId
		this.resultHandled = true
		this.bufferedText = ''
		this.bufferedFileId = ''

		if (typeof this.onResult === 'function') {
			this.onResult({
				fileId: nextFileId,
				text: nextText,
			})
		}
	}

	connect() {
		if (!this.recordId) return
		if (this.socket && (this.socket.readyState === WebSocket.OPEN || this.socket.readyState === WebSocket.CONNECTING)) {
			return
		}

		const socketUrl = `${process.env.VUE_APP_SOCKET}svc-aicoachplus-websocket/phone_call/pas/${this.recordId}:2?lang=${encodeURIComponent(readLang())}`
		this.isClosing = false
		this.socket = new WebSocket(socketUrl)

		this.socket.onopen = () => {
			this.sendRaw({
				action: 1,
				action_data: {
					authN: readToken(),
					lang: readLang(),
					sampleRate: this.sampleRate,
				},
			})

			const queued = [...this.pendingMessages]
			this.pendingMessages = []
			queued.forEach((message) => this.sendRaw(message))
		}

		this.socket.onmessage = (event) => {
			this.handleMessage(event.data)
		}

		this.socket.onerror = () => {
			if (typeof this.onError === 'function') {
				this.onError(new Error(DEFAULT_ERROR_MESSAGE))
			}
		}

		this.socket.onclose = () => {
			this.socket = null
			if (!this.isClosing) {
				window.setTimeout(() => this.connect(), 300)
			}
		}
	}

	handleMessage(rawMessage) {
		let payload = null
		try {
			payload = JSON.parse(rawMessage || '{}')
		} catch (error) {
			return
		}

		const { action, action_data = {} } = payload
		if (action === 26) {
			const partialText = toText(typeof action_data === 'string'
				? action_data
				: (action_data.text ?? action_data.Text ?? action_data.audioText ?? action_data.AudioText ?? ''))
			if (!partialText) return
			this.bufferedText = `${this.bufferedText}${partialText}`
			if (typeof this.onPartial === 'function') {
				this.onPartial(partialText)
			}
			return
		}

		if (action === 24) {
			const fileId = toText(action_data.file_id ?? action_data.fileId ?? action_data.FileId)
			const text = toText(action_data.audioText ?? action_data.AudioText ?? action_data.text ?? action_data.Text)
			if (fileId) {
				this.bufferedFileId = fileId
			}
			this.emitBufferedResult(text, fileId)
			return
		}

		if (action === 11) {
			const fallbackResult = this.consumeBufferedResult()
			if (fallbackResult.text) {
				if (typeof this.onResult === 'function') {
					this.onResult(fallbackResult)
				}
				return
			}
			if (typeof this.onError === 'function') {
				this.onError(new Error(DEFAULT_ERROR_MESSAGE))
			}
			return
		}

		if (action !== 30) return

		let content = {}
		try {
			content = JSON.parse(action_data.Content || action_data.content || '{}')
		} catch (error) {
			content = {}
		}

		switch (content.type) {
			case '30_2':
				this.resetBufferedResult()
				if (typeof this.onRecordingStart === 'function') {
					this.onRecordingStart()
				}
				break
			case '30_6':
				if (typeof this.onError === 'function') {
					this.onError(new Error(RECORD_ERROR_MESSAGE))
				}
				break
			default:
				break
		}
	}

	sendRaw(message) {
		if (!message) return
		if (!this.socket || this.socket.readyState !== WebSocket.OPEN) {
			this.pendingMessages.push(message)
			this.connect()
			return
		}
		this.socket.send(JSON.stringify(message))
	}

	sendCommand(type) {
		this.sendRaw({
			action: 30,
			action_data: {
				content: JSON.stringify({ type }),
			},
		})
	}

	getBufferedResult() {
		return {
			fileId: this.bufferedFileId,
			text: this.bufferedText,
		}
	}

	start() {
		this.resetBufferedResult()
		this.sendCommand('30_1')
	}

	stop() {
		this.sendCommand('30_3')
	}

	cancel() {
		this.resultHandled = true
		this.sendCommand('30_5')
	}

	close() {
		this.isClosing = true
		this.pendingMessages = []
		if (this.socket) {
			this.socket.close()
			this.socket = null
		}
	}
}
