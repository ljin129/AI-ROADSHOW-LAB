/*
 * @Author: your name
 * @Date: 2022-02-22 11:01:55
 * @LastEditTime: 2025-06-19 17:33:10
 * @LastEditors: 黄永星 huangyongxing@betawm.com
 * @Description: 打开koroFileHeader查看配置 进行设置: https://github.com/OBKoro1/koro1FileHeader/wiki/%E9%85%8D%E7%BD%AE
 * @FilePath: \Beta.AIRelationH5\airelationh5\src\axios\request.js
 */
import axios from 'axios'

const instance = axios.create({
	// timeout: 30000,
	timeout: 120000,
	baseURL: '',
})
axios.defaults.headers.post['Content-Type'] = 'application/json;utf-8'

//请求拦截器
instance.interceptors.request.use(
	(config) => {
		if (config.method === 'formdata') {
			config.headers.post['Content-Type'] = 'multipart/form-data;charset=utf-8'
			config.method = 'post'
			// 兼容某些老接口的入参需要特殊的入参处理(.eg: AddMyOption)
			const fPar = config.otherPar == 'useSearch' ? new URLSearchParams() : new FormData()
			for (const key in config.params) {
				fPar.append(key, config.params[key])
			}
			config.data = fPar
			config.params = ''
		} else if (config.method === 'query') {
			config.method = 'post'
			config.data = ''
		} else if (config.method === 'post') {
			config.data = config.params
			config.params = ''
		}
		config.headers['Cache-Control'] = 'no-cache'
		// if (window.bjsdk.query('preview') == 1) {
		// 	config.headers['Authorization'] = window.bjsdk.gJWT()
		// }
		return config
	},
	(err) => {
		return Promise.reject(err)
	}
)

//响应拦截器
instance.interceptors.response.use(
	(res) => {
		if (res.status === 200) {
			return res.data
		}
		return addErrorLog(res.data)
	},
	(err) => {
		return addErrorLog(err)
	}
)
const addErrorLog = (res) => {
	console.error(res, 'axios/error')
	return Promise.reject(res)
}
const $axios = (option) => {
	const parDef = {
		url: '',
		data: {},
		method: 'post',
	}
	const par = Object.assign(parDef, option)
	return new Promise((resolve, reject) => {
		instance({
			url: par.url,
			params: par.data,
			method: par.method,
			otherPar: par.otherPar || '', // 兼容某些老接口的入参需要特殊的入参处理(.eg: AddMyOption)
			onUploadProgress: par.onUploadProgress,
		})
			.then((res) => {
				resolve(res)
			})
			.catch((err) => {
				reject(err)
			})
	})
}
export default $axios
