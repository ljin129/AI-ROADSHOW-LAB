/** 公共api存放位置 */
import { getData } from '@/axios/axios'

//上传图片(type=uploadimage)、上传文件(type=uploadfile)
/**
 * 上传图片文件
 * @param type: 上传类型 uploadimage -> 图片; uploadfile -> 文件
 * @param modelID: 上传文件服务需要的模块id，需要找崔添加 ai教练：modelID=61015
 * @param splitimg: 是否需要文件预览图 
 * @param storage: 文件存储类型  ai教练：Storage=aicoach
 * **/
export const uploadFiles = (params) => {
	let splitimg = params.splitimg?'&splitimg=1' : ''
	return new Promise((resolve, reject) => {
		getData(`/Beta.FileServiceV2/Api/UserFile?modelID=${params.modelID || '61015'}&type=${params.type||'uploadimage'}&Storage=${params.storage || 'aicoach'}${splitimg}`, params, "formdata")
			.then((res) => {
				resolve(res)
			})
			.catch((err) => {
				reject(err)
			})
	})
}
