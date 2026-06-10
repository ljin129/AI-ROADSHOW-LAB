/**
 * 深度遍历对象并替换自定标签与之对应的闭合标签，兼容替换成空字符串
 * @param {*} obj
 * @param {*} targetTag
 * @param {*} replacementTag
 */
export const deepReplaceTags = (obj, targetTag, replacementTag) => {
	if (typeof obj === 'string') {
		// 如果是字符串，直接替换
		return obj.replace(new RegExp(`<${targetTag}[^>]*>`, 'g'), replacementTag.trim() === '' ? '' : `<${replacementTag}>`).replace(new RegExp(`</${targetTag}>`, 'g'), replacementTag.trim() === '' ? '' : `</${replacementTag}>`)
	} else if (Array.isArray(obj)) {
		// 如果是数组，递归处理每个元素
		return obj.map((item) => deepReplaceTags(item, targetTag, replacementTag))
	} else if (typeof obj === 'object' && obj !== null) {
		// 如果是对象，递归处理每个属性
		const newObj = {}
		for (const key in obj) {
			newObj[key] = deepReplaceTags(obj[key], targetTag, replacementTag)
		}
		return newObj
	}
	return obj // 如果不是字符串、数组或对象，直接返回原值
}

/**
 * 去除除HTML标签，返回纯文本
 * @param {*} htmlString
 * @returns
 */
export const stripHtmlTags = (htmlString) => {
	if (!htmlString || typeof htmlString !== 'string') {
		return ''
	}
	try {
		// 使用 DOMParser 解析HTML
		const parser = new DOMParser()
		const parsedHtml = parser.parseFromString(htmlString, 'text/html')
		// 返回纯文本内容
		return parsedHtml.body.textContent || ''
	} catch (error) {
		console.error('Error stripping HTML tags:', error)
		// 出错时降级使用正则表达式
		return htmlString.replace(/<[^>]*>/g, '')
	}
}

/**
 * 深度遍历对象并除HTML标签，返回纯文本
 * @param {*} data
 * @param {*} options
 *   - excludeKeys: 排除处理的键名数组，默认不排除任何键
 *   - maxDepth: 最大递归深度，防止过深嵌套，默认20层
 * @returns
 */
export const deepStripHtmlTags = (data, options = {}) => {
	const { excludeKeys = [], maxDepth = 20 } = options
	// 内部递归函数
	function _deepStrip(currentData, currentDepth = 0, seen = new WeakMap()) {
		// 检查最大深度
		if (currentDepth > maxDepth) {
			return currentData
		}

		// 处理基本类型
		if (currentData === null || typeof currentData !== 'object') {
			if (typeof currentData === 'string') {
				return stripHtmlTags(currentData)
			}
			return currentData
		}

		// 检查循环引用
		if (seen.has(currentData)) {
			return seen.get(currentData)
		}

		// 处理数组
		if (Array.isArray(currentData)) {
			const newArray = []
			seen.set(currentData, newArray)
			for (let i = 0; i < currentData.length; i++) {
				newArray[i] = _deepStrip(currentData[i], currentDepth + 1, seen)
			}
			return newArray
		}

		// 处理对象
		if (typeof currentData === 'object') {
			const newObj = {}
			seen.set(currentData, newObj)
			for (const [key, value] of Object.entries(currentData)) {
				// 跳过排除的键
				if (excludeKeys.includes(key)) {
					newObj[key] = value
					continue
				}
				newObj[key] = _deepStrip(value, currentDepth + 1, seen)
			}
			return newObj
		}
		return currentData
	}
	return _deepStrip(data)
}

/* *****  PropertyProcessor Start ***** */

// 接受多个策略函数，对对象的字符串属性进行处理
class PropertyProcessor {
	constructor(strategies = []) {
		this.strategies = strategies
	}
	// 增加策略
	addStrategy(strategy) {
		this.strategies.push(strategy)
	}
	// 遍历执行所有策略
	processValue(value) {
		let result = value
		for (const strategy of this.strategies) {
			result = strategy(result)
		}
		return result
	}
	processObject(obj) {
		// 非法值 直接返回
		if (!obj) return obj
		// 如果是字符串 直接执行策略，返回结果
		if (typeof obj === 'string') return this.processValue(obj)
		// 针对数组处理
		if (Array.isArray(obj)) {
			return obj.map((item) => this.processObject(item))
		}
		// 针对对象处理
		if (typeof obj === 'object') {
			const newObj = {}
			for (const key in obj) {
				if (!Object.prototype.hasOwnProperty.call(obj, key)) continue
				newObj[key] = this.processObject(obj[key])
			}
			return newObj
		}
		return obj
	}
}

// 策略1：去掉 <strong> 标签
const removeStrongTag = (str) => {
	return str.replace(/<strong>|<\/strong>/gi, '')
}

// 策略2：<br> 替换为 \n
const brToNewline = (str) => {
	return str.replace(/<br\s*\/?\s*>/gi, '\n')
}

// 策略3：删除 <s> 标签和其中内容
const removeSTagAndContent = (str) => {
	return str.replace(/<s>[\s\S]*?<\/s>/gi, '')
}

// 主方法：传入对象，依次执行策略，返回处理后的对象
export const processObjectProperties = (obj) => {
	const processor = new PropertyProcessor([removeStrongTag, brToNewline, removeSTagAndContent])
	return processor.processObject(obj)
}

/* *****  PropertyProcessor End ****** */

/**
 * 根据语言类型统计字符数或单词数
 * @param {*} inputString  输入字符串
 * @param {*} languageType 语言类型 1 中文 2 英文
 * @returns
 */
export const countCharactersOrWords = (inputString, maxLen, languageType) => {
	if (languageType === 2) {
		// 英文按单词数计算，中文按字符数计算
		const pattern = /[\u4e00-\u9fff]|[A-Za-z]+/g
		const len = (inputString.match(pattern) || []).length

		let lastIndex = inputString.length
		if (len >= maxLen) {
			let match
			let count = 0
			while ((match = pattern.exec(inputString)) !== null && count < maxLen) {
				count++
				lastIndex = match.index + match[0].length
			}
		}
		return {
			length: len < maxLen ? len : maxLen,
			result: inputString.substring(0, lastIndex + 1),
		}
	} else {
		// 其他都按中文处理，返回字符数
		return {
			length: inputString.length < maxLen ? inputString.length : maxLen,
			result: inputString.substring(0, maxLen),
		}
	}
}

//判断变量是否是ArrayBuffer类型
export const IsArrayBuffer = (obj) => {
	return Object.prototype.toString.call(obj) === '[object ArrayBuffer]'
}

// 创建audioContext
export const createAudioContext = () => {
	const audioContext = new (window.AudioContext || window.webkitAudioContext)()
	audioContext.resume()
	return audioContext
}

export const concatArrayBuffers = (buffers) => {
	// 参数验证
	if (!Array.isArray(buffers)) {
		throw new TypeError('Expected buffers to be an array')
	}

	const views = []
	let totalLength = 0

	for (let i = 0; i < buffers.length; i++) {
		const buffer = buffers[i]

		// 跳过空值
		if (buffer == null) continue

		let view
		try {
			if (buffer instanceof ArrayBuffer) {
				view = new Uint8Array(buffer)
			} else if (ArrayBuffer.isView(buffer)) {
				// 精确处理各种 TypedArray
				view = new Uint8Array(buffer.buffer, buffer.byteOffset, buffer.byteLength)
			} else {
				console.warn(`concatArrayBuffers: 索引 ${i} 处忽略不支持的类型`, typeof buffer)
				continue
			}

			// 验证视图有效性
			if (view.byteLength === 0) continue

			views.push(view)
			totalLength += view.byteLength
		} catch (error) {
			console.warn(`concatArrayBuffers: 处理索引 ${i} 的 buffer 时出错:`, error)
			continue
		}
	}

	if (totalLength === 0) {
		return new ArrayBuffer(0)
	}

	// 合并数据
	const result = new Uint8Array(totalLength)
	let offset = 0

	for (const view of views) {
		result.set(view, offset)
		offset += view.byteLength
	}

	return result.buffer
}

// ArrayBuffer 转 Base64
export const arrayBufferToBase64 = (buffer) => {
	let binary = ''
	const bytes = new Uint8Array(buffer)
	const len = bytes.byteLength

	for (let i = 0; i < len; i++) {
		binary += String.fromCharCode(bytes[i])
	}

	return btoa(binary)
}

// Base64 转 ArrayBuffer
export const base64ToArrayBuffer = (base64) => {
	const binaryString = atob(base64)
	const len = binaryString.length
	const bytes = new Uint8Array(len)

	for (let i = 0; i < len; i++) {
		bytes[i] = binaryString.charCodeAt(i)
	}

	return bytes.buffer
}

/**
 * 根据环境变量，返回完整地址
 * @param {*} ''
 * @returns
 */
export const getFullUrl = (path, width = window.screen.width * 2) => {
	if (!path) return ''
	let result = ''
	if (path.startsWith('http') || path.startsWith('//')) {
		result = path
	} else {
		const baseUrl = process.env.VUE_APP_CDN || ''
		result = `${baseUrl}/Beta.FileService/f/${path}`
	}
	if (isImageUrl(result) && width > 0) {
		// 如果是图片链接，添加宽度参数，判断有没有其他参数，有则用&连接
		// width <= 0，则不追加宽度参数(合成视频时，APP端要求去掉该参数)
		const separator = /\?[^#]+/.test(result) ? '&' : '?'
		result += `${separator}datatype=stream&width=${width}`
	}
	return result
}

// 正则判断是否是文件URL链接
const isImageUrl = (url) => {
	const imageExtensions = /\.(jpg|jpeg|png|gif|webp|bmp|svg|ico)(\?.*)?$/i
	return imageExtensions.test(url)
}

/**
 * 提取字符串中第一个出现的 JSON 对象或数组
 * @param {string} input - 输入的字符串
 * @returns {string|''} - 匹配到的 JSON 字符串或 null
 */
export const extractFirstJson = (input) => {
	// 空字符串保护 (等同于 string.IsNullOrEmpty)
	if (!input) {
		return ''
	}

	// 根据最先出现的起始符决定匹配对象还是数组
	const startObj = input.indexOf('{')
	const startArr = input.indexOf('[')

	if (startObj === -1 && startArr === -1) {
		return ''
	}

	// 构造从起始符开始的子串，避免匹配到后续片段
	const useObject = startObj !== -1 && (startArr === -1 || startObj < startArr)
	const start = useObject ? startObj : startArr
	const segment = input.substring(start)

	// 使用正则进行贪婪匹配，跨行匹配；锚定到子串起始
	// 注意：JavaScript 中的 [\s\S] 原生支持跨行匹配，无需类似 C# 的 RegexOptions.Singleline
	const pattern = useObject ? /^\{[\s\S]*\}/ : /^\[[\s\S]*\]/
	const match = segment.match(pattern)

	if (match) {
		return match[0] // match[0] 对应 C# 中的 match.Value
	}

	// 未能匹配完整 JSON 片段
	return ''
}
