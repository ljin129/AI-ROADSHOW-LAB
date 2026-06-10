const ROADSHOW_API_ROOT = '//{web}/Beta.AIRoadshow.H5Api'
const RELATION_API_ROOT = '//{web}/Beta.AIRelation.H5Api'
const ID_KEY_PATTERN = /(?:^|_)(?:[a-z0-9]+_)*[a-z0-9]*id$/i

const getBjsdk = () => window.bjsdk || {}

const buildUrl = (path, query = null) => {
	const bjsdk = getBjsdk()
	const rawUrl = `${path.startsWith('//') ? '' : ''}${path}`
	const normalizedQuery = query && typeof query === 'object' ? normalizeRequestLongFields(query) : query
	if (bjsdk && typeof bjsdk.url === 'function') {
		return bjsdk.url(rawUrl, true, normalizedQuery || {})
	}

	const url = new URL(rawUrl, window.location.origin)
	if (normalizedQuery && typeof normalizedQuery === 'object') {
		Object.keys(normalizedQuery).forEach((key) => {
			const value = normalizedQuery[key]
			if (value === undefined || value === null || value === '') return
			url.searchParams.set(key, value)
		})
	}
	return url.toString()
}

const buildHeaders = (extraHeaders = {}) => {
	const bjsdk = getBjsdk()
	const token = bjsdk && typeof bjsdk.gLS === 'function' ? bjsdk.gLS('bjwt') : ''
	const headers = {
		'Cache-Control': 'no-cache',
		...extraHeaders,
	}

	if (token) {
		headers.Authorization = token
	}

	return headers
}

const getErrorMessage = (payload, fallback) => {
	if (!payload) return fallback
	if (typeof payload === 'string' && payload.trim()) return payload
	if (typeof payload === 'object') {
		return payload.ErrorMessage || payload.errorMessage || payload.message || fallback
	}
	return fallback
}

const shouldTreatAsIdKey = (key) => {
	return !!key && ID_KEY_PATTERN.test(String(key || ''))
}

const normalizeRequestLongFields = (value, parentKey = '') => {
	if (value === null || value === undefined) return value

	if (Array.isArray(value)) {
		return value.map((item) => normalizeRequestLongFields(item, parentKey))
	}

	if (typeof value === 'object') {
		return Object.keys(value).reduce((result, key) => {
			result[key] = normalizeRequestLongFields(value[key], key)
			return result
		}, {})
	}

	if (shouldTreatAsIdKey(parentKey)) {
		return String(value)
	}

	return value
}

const normalizeLongIdFields = (rawText) => {
	return String(rawText || '').replace(/"([^"]+)"\s*:\s*(-?\d+)(?=\s*[,}\]])/g, (match, key, value) => {
		if (!shouldTreatAsIdKey(key)) return match
		return `"${key}":"${value}"`
	})
}

const parseJsonPreservingLongIds = (rawText) => {
	const safeText = normalizeLongIdFields(rawText)
	return JSON.parse(safeText)
}

const buildRequestBody = (payload) => {
	if (!payload || typeof payload !== 'object') return undefined
	return JSON.stringify(normalizeRequestLongFields(payload))
}

const parseResponsePayload = async (response, fallbackMessage) => {
	if (!response.ok) {
		throw new Error(`${fallbackMessage}（HTTP ${response.status}）`)
	}

	const rawText = await response.text()
	const payload = rawText ? parseJsonPreservingLongIds(rawText) : {}
	const state = payload && (payload.State !== undefined ? payload.State : payload.state)
	if (state !== 0) {
		throw new Error(getErrorMessage(payload, fallbackMessage))
	}

	return payload
}

const requestJson = async ({
	url,
	method = 'GET',
	query = null,
	body = null,
	fallbackMessage = '请求失败',
}) => {
	const response = await fetch(buildUrl(url, query), {
		method,
		headers: buildHeaders({
			Accept: 'application/json',
			...(body ? { 'Content-Type': 'application/json;charset=utf-8' } : {}),
		}),
		body: body ? buildRequestBody(body) : undefined,
	})

	return parseResponsePayload(response, fallbackMessage)
}

const parseSseBlock = (block) => {
	const lines = String(block || '')
		.replace(/\r/g, '')
		.split('\n')

	let eventName = 'message'
	const dataLines = []

	lines.forEach((line) => {
		if (!line) return
		if (line.startsWith('event:')) {
			eventName = line.slice(6).trim() || 'message'
			return
		}
		if (line.startsWith('data:')) {
			dataLines.push(line.slice(5).trimStart())
		}
	})

	const rawData = dataLines.join('\n')
	if (!rawData) {
		return { event: eventName, data: null, raw: '' }
	}

	try {
		return {
			event: eventName,
			data: parseJsonPreservingLongIds(rawData),
			raw: rawData,
		}
	} catch (error) {
		return {
			event: eventName,
			data: rawData,
			raw: rawData,
		}
	}
}

export const OpenPracticeChatStream = ({ payload = {}, onEvent = null, onError = null } = {}) => {
	const controller = new AbortController()
	const streamUrl = buildUrl(`${ROADSHOW_API_ROOT}/Practice/PracticeChatStream`)

	const finished = (async () => {
		const response = await fetch(streamUrl, {
			method: 'POST',
			headers: buildHeaders({
				Accept: 'text/event-stream',
				'Content-Type': 'application/json;charset=utf-8',
			}),
			body: buildRequestBody(payload || {}),
			signal: controller.signal,
		})

		if (!response.ok || !response.body) {
			throw new Error(`路演对话请求失败（HTTP ${response.status}）`)
		}

		const reader = response.body.getReader()
		const decoder = new TextDecoder('utf-8')
		let buffer = ''

		while (true) {
			const { done, value } = await reader.read()
			if (done) break

			buffer += decoder.decode(value, { stream: true })

			let boundaryIndex = buffer.indexOf('\n\n')
			while (boundaryIndex >= 0) {
				const block = buffer.slice(0, boundaryIndex)
				buffer = buffer.slice(boundaryIndex + 2)

				if (block.trim()) {
					const parsed = parseSseBlock(block)
					if (typeof onEvent === 'function') {
						onEvent(parsed)
					}
				}

				boundaryIndex = buffer.indexOf('\n\n')
			}
		}

		const tail = buffer.trim()
		if (tail && typeof onEvent === 'function') {
			onEvent(parseSseBlock(tail))
		}
	})()

	const guardedFinished = finished.catch((error) => {
		if (controller.signal.aborted) return
		if (typeof onError === 'function') {
			onError(error)
			return
		}
		throw error
	})

	return {
		controller,
		finished: guardedFinished,
	}
}

export const GetPublishedActivityList = (params = {}) => {
	return requestJson({
		url: `${ROADSHOW_API_ROOT}/Activity/GetPublishedActivityList`,
		query: params,
		fallbackMessage: '获取活动列表失败',
	})
}

export const GetActivityDetail = (params = {}) => {
	return requestJson({
		url: `${ROADSHOW_API_ROOT}/Activity/GetActivityDetail`,
		query: params,
		fallbackMessage: '获取活动详情失败',
	})
}

export const GetAbilityDimensionListByCustCompanyId = (params = {}) => {
	return requestJson({
		url: `${ROADSHOW_API_ROOT}/AbilityDimension/GetAbilityDimensionListByCustCompanyId`,
		query: params,
		fallbackMessage: '获取能力模型失败',
	})
}

export const StartPractice = (params = {}) => {
	return requestJson({
		url: `${ROADSHOW_API_ROOT}/Practice/StartPractice`,
		method: 'POST',
		body: params,
		fallbackMessage: '创建演练记录失败',
	})
}

export const GetPracticeDetail = (params = {}) => {
	return requestJson({
		url: `${ROADSHOW_API_ROOT}/Practice/GetPracticeDetail`,
		query: params,
		fallbackMessage: '获取演练明细失败',
	})
}

export const GetRoadshowAsrProvider = (params = {}) => {
	return requestJson({
		url: `${RELATION_API_ROOT}/LLMChat/GetAsrProvider`,
		query: params,
		fallbackMessage: '获取语音配置失败',
	})
}
