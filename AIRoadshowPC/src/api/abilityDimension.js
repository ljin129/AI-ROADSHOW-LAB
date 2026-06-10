import { getData } from '@/axios/axios'

const request = (promise) =>
	new Promise((resolve, reject) => {
		promise
			.then((res) => {
				if (res.State === 0 || res.state === 0) {
					resolve(res)
				} else {
					reject(res)
				}
			})
			.catch((err) => {
				reject(err)
			})
	})

export const GetAbilityDimensionTree = (params = {}) =>
	request(getData('/Beta.AIRoadshow.PCApi/AbilityDimension/GetAbilityDimensionTree', params, 'get'))

export const SaveAbilityDimension = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/AbilityDimension/SaveAbilityDimension', params, 'post'))

export const UpdateAbilityDimension = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/AbilityDimension/UpdateAbilityDimension', params, 'post'))

export const RemoveAbilityDimension = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/AbilityDimension/RemoveAbilityDimension', params, 'post'))
