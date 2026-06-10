/*
 * @Author: your name
 * @Date: 2022-02-22 11:01:55
 * @LastEditTime: 2022-03-28 10:07:06
 * @LastEditors: Please set LastEditors
 * @Description: 打开koroFileHeader查看配置 进行设置: https://github.com/OBKoro1/koro1FileHeader/wiki/%E9%85%8D%E7%BD%AE
 * @FilePath: \Beta.AIRelationH5\airelationh5\src\axios\axios.js
 */
import $axios from '@/axios/request'
/**
 *
 * @param {*} url --api路径
 * @param {*} data -- 请求参数 json对象
 * @param {*} method --请求方式：get/post/formdata
 * @param {*} otherPar --兼容某些老接口的入参需要特殊的入参处理(.eg: AddMyOption)
 */
export const getData = (
    url = '',
    data = {},
    method = 'post',
    otherPar = '',
    onUploadProgress = null
) => {
    url = setBaseUrl(url)
    return $axios({
        url,
        method,
        data,
        otherPar,
        onUploadProgress,
    })
}

//处理api请求的url
const bsdk = window.bjsdk
const setBaseUrl = (url) => {
    console.log('lang===',bsdk.query('lang'))
    if (/^(\/Beta|Beta)/i.test(url) || /^[a-z]/i.test(url) && !/^(http)/i.test(url)) {
        //以'/beta'或者‘beta’开头
        //以字母开头且不是‘http’开头的，如：‘videosettings/api’、‘/wolf-busi-burstinfoapi/interpret/’项目，前面加上‘/’在转为绝对路径
        return window.bjsdk.url(`//{web}/${url}`,true)
    }
    return window.bjsdk.url(url,true)
}
