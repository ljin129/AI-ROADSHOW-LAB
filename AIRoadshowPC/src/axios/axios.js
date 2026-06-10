const $axios = window.bjsdk._ext.$axios;
const setBaseUrl = window.bjsdk._ext.setBaseUrl;

//本地调试使用 处理401问题
const _GetLocalUrl = (url) => {
	// 如果传入的是完整URL，则直接返回window.bjsdk.url(url,true)
	if(/^https?:\/\//i.test(url)){
		return window.bjsdk.url(url,true);
	}
	if(location.port || location.hostname=='localhost' || /^(\d{1,3}\.){3}(\d{1,3})$/.test(location.hostname)){
		url = window.bjsdk.url(`${process.env.VUE_APP_BASEURL}/${url}`,true)
	}
	return url;
}
/**
 *
 * @param {*} url --api路径
 * @param {*} data -- 请求参数 json对象
 * @param {*} method --请求方式：get/post/formdata
 * @param {*} config --向请求头中添加自定义参数  如：
 * 					 --timeout : 当前接口超时时间，为0时--默认走全局项目配置的，大于0取当前设置的超时时间
 * 					 --responseType : 当前接口返回类型， 'json'(默认), 'blob'(下载文件流)，'arraybuffer', 'document',  'text', 'stream'，
 */
export const getData = (url = '', data = {}, method='post', config={}) => {
	url = _GetLocalUrl(url);
	return $axios({
		url:setBaseUrl(url),
		method,
		data,
		config
	})
};

/**
 *
 * @param {*} url --api路径
 * @param {*} id  --路径参字符串，通常情况是以斜杠"/"分割的多个字符串
 *                  一个字符串时不用斜杠"/"
 *                  如："aaa" 和 "aaa/bbb/ccc"
 *
 * @param {*} data -- 请求参数 json对象
 * @param {*} method --请求方式：get/post/formdata
 * @param {*} config --向请求头中添加自定义参数  如：
 * 					 --timeout : 当前接口超时时间，为0时--默认走全局项目配置的，大于0取当前设置的超时时间
 * 					 --responseType : 当前接口返回类型， 'json'(默认), 'blob'(下载文件流)，'arraybuffer', 'document',  'text', 'stream'，
 */
export const requstUrl = (url = '', id = '', data = {}, method = 'post', config={}) => {
	url = _GetLocalUrl(url);
	return $axios({
		url: `${setBaseUrl(url)}${id ? '/' + id:''}`,
		method,
		data,
		config
	})
};


/**
 *
 * @param {*} url --api路径
 * @param {*} data -- 请求参数 json对象
 * @param {*} method --请求方式：get/post/formdata
 * @param {*} config --向请求头中添加自定义参数  如：
 * 					 --timeout : 当前接口超时时间，为0时--默认走全局项目配置的，大于0取当前设置的超时时间
 * 					 --responseType : 当前接口返回类型， 'json'(默认), 'blob'(下载文件流)，'arraybuffer', 'document',  'text', 'stream'，
 */
export const fileExports = (url = '', data = {}, method, config={responseType:'blob'}) => {
	url = _GetLocalUrl(url);
	return $axios({
		url:setBaseUrl(url),
		method,
		data,
		config
	})
};

export default {
	getData,
	requstUrl,
	fileExports
};
