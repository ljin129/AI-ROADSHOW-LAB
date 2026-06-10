import { getData } from '@/axios/axios'

const getResponseState = (res) => res?.State ?? res?.state

const request = (promise) =>
	new Promise((resolve, reject) => {
		promise
			.then((res) => {
				if (getResponseState(res) === 0) {
					resolve(res)
				} else {
					reject(res)
				}
			})
			.catch((err) => {
				reject(err)
			})
	})

const buildQueryString = (params = {}) => {
	const search = new URLSearchParams()
	Object.keys(params).forEach((key) => {
		const value = params[key]
		if (value === '' || value === null || value === undefined) {
			return
		}
		search.append(key, value)
	})
	const query = search.toString()
	return query ? `?${query}` : ''
}

export const GetActivityList = (params = {}) =>
	request(
		getData(`/Beta.AIRoadshow.PCApi/Activity/GetActivityList${buildQueryString(params)}`, {}, 'get')
	)

export const GetActivityStatistics = () =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/GetActivityStatistics', {}, 'get'))

export const ExtractActivityInfo = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/ExtractActivityInfo', params, 'post'))

export const GetExtractActivityInfoResult = (taskId) =>
	request(
		getData(
			`/Beta.AIRoadshow.PCApi/Activity/GetExtractActivityInfoResult${buildQueryString({ taskId })}`,
			{},
			'get'
		)
	)

export const ExtractQuestionInfo = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/ExtractQuestionInfo', params, 'post'))

export const GetExtractQuestionInfoResult = (params = {}) =>
	request(
		getData(
			`/Beta.AIRoadshow.PCApi/Activity/GetExtractQuestionInfoResult${buildQueryString(params)}`,
			{},
			'get'
		)
	)

export const GetActivityDetail = (activityId) =>
	request(
		getData(
			`/Beta.AIRoadshow.PCApi/Activity/GetActivityDetail${buildQueryString({ activityId })}`,
			{},
			'get'
		)
	)

export const GetActivityQuestionPage = (params = {}) =>
	request(
		getData(
			`/Beta.AIRoadshow.PCApi/Activity/GetActivityQuestionPage${buildQueryString(params)}`,
			{},
			'get'
		)
	)

export const GetActivityBusinessStageList = (activityId) =>
	request(
		getData(
			`/Beta.AIRoadshow.PCApi/Activity/GetActivityBusinessStageList${buildQueryString({ activityId })}`,
			{},
			'get'
		)
	)

export const UpdateActivity = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/UpdateActivity', params, 'post'))

export const SaveRole = (params) => request(getData('/Beta.AIRoadshow.PCApi/Activity/SaveRole', params, 'post'))

export const UpdateRole = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/UpdateRole', params, 'post'))

export const SaveBusinessStage = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/SaveBusinessStage', params, 'post'))

export const UpdateBusinessStage = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/UpdateBusinessStage', params, 'post'))

export const SaveQuestion = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/SaveQuestion', params, 'post'))

export const UpdateQuestion = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/UpdateQuestion', params, 'post'))

export const RemoveActivity = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/RemoveActivity', params, 'post'))

export const RemoveRole = (params) => request(getData('/Beta.AIRoadshow.PCApi/Activity/RemoveRole', params, 'post'))

export const RemoveBusinessStage = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/RemoveBusinessStage', params, 'post'))

export const RemoveQuestion = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/RemoveQuestion', params, 'post'))

export const BatchRemoveQuestion = (params) =>
	request(getData('/Beta.AIRoadshow.PCApi/Activity/BatchRemoveQuestion', params, 'post'))
