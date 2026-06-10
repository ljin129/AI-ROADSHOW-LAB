<template>
	<div class="activity-management-page">
		<div v-loading="pageLoading || statisticsLoading" class="loading-mask-host">
		<section v-if="currentPage === 'list'" class="page-section">
			<div class="page-header">
				<div>
					<div class="page-title">路演活动管理列表</div>
					<div class="page-subtitle">支持活动列表查询、活动配置编辑以及角色、业务环节、题库配置。</div>
				</div>
			</div>

			<activity-stats :stats="stats" />

			<div class="page-card">
				<activity-search-bar
					:search-form="searchForm"
					@create="newActivity"
					@search="handleSearch"
					@reset="resetSearch"
				/>
				<activity-table :activities="filteredActivities" @edit="editActivity" @delete="handleDeleteActivityRemote" />
			</div>
		</section>

		<section v-else class="page-section">
			<div class="page-header">
				<div>
					<div class="page-title">{{ createPageTitle }}</div>
					<div class="page-subtitle">{{ createPageSubtitle }}</div>
				</div>
				<el-button size="small" @click="backToList">返回列表</el-button>
			</div>

			<div class="page-card">
				<el-steps :active="displayActiveStep" finish-status="success" align-center class="stepper">
					<el-step v-if="!editMode" title="基础资料" />
					<el-step title="活动配置" />
					<el-step title="角色配置" />
					<el-step title="业务环节配置" />
					<el-step title="题库配置" />
				</el-steps>

				<activity-basic-info-form
					v-show="activeStep === 0"
					:form="draft.form"
					:uploading="uploading"
					@upload-file="handleUploadFile"
					@update-attachments="updateAttachments"
				/>
				<activity-config-form
					v-show="activeStep === 1"
					:form="draft.form"
					:uploading="uploading"
					@upload-cover="handleUploadCoverImage"
					@update-cover="updateCoverImage"
				/>
				<role-manager
					v-show="activeStep === 2"
					:roles.sync="draft.roles"
					@save-role="handleRoleSave"
					@delete-role="handleRoleDelete"
				/>
				<scene-manager
					v-show="activeStep === 3"
					:scenes.sync="draft.scenes"
					@save-scene="handleSceneSave"
					@delete-scene="handleSceneDelete"
				/>
				<question-manager
					v-show="activeStep === 4"
					:questions.sync="draft.questions"
					:scenes="draft.scenes"
					:dimensions="dimensions"
					@save-question="handleQuestionSave"
					@delete-question="handleQuestionDelete"
					@delete-questions="handleQuestionBatchDelete"
				/>

				<div class="footer-actions">
					<el-button v-if="activeStep > minStep" size="small" @click="previousStep">上一步</el-button>
					<el-button type="primary" size="small" @click="nextStep">
						{{ resolvedNextButtonText() }}
					</el-button>
				</div>
			</div>
		</section>
		</div>
	</div>
</template>

<script>
import {
	ExtractActivityInfo,
	ExtractQuestionInfo,
	BatchRemoveQuestion,
	GetActivityBusinessStageList,
	GetActivityDetail,
	GetActivityList,
	GetActivityQuestionPage,
	GetActivityStatistics,
	GetExtractActivityInfoResult,
	GetExtractQuestionInfoResult,
	RemoveActivity,
	RemoveBusinessStage,
	RemoveQuestion,
	RemoveRole,
	SaveBusinessStage,
	SaveQuestion,
	SaveRole,
	UpdateActivity,
	UpdateBusinessStage,
	UpdateQuestion,
	UpdateRole,
} from '@/api/activityManagement'
import ActivityBasicInfoForm from './components/ActivityBasicInfoForm.vue'
import ActivityConfigForm from './components/ActivityConfigForm.vue'
import ActivitySearchBar from './components/ActivitySearchBar.vue'
import ActivityStats from './components/ActivityStats.vue'
import ActivityTable from './components/ActivityTable.vue'
import QuestionManager from './components/QuestionManager.vue'
import RoleManager from './components/RoleManager.vue'
import SceneManager from './components/SceneManager.vue'
import {
	cloneDraft,
	createDefaultActivityForm,
	createDraftFromActivity,
	createEmptyDraft,
	dimensionOptions,
} from './mock'

const formatDateValue = (value) => new Date(value).getTime()
const POLL_INTERVAL = 5000
const DEFAULT_ACTIVITY_QUESTION_PAGE_SIZE = 200
const MATERIAL_TYPE_QUESTION = 'question'
const LAST_STEP = 4

const getResponseData = (res) => res?.Data ?? res?.data ?? {}
const getErrorMessage = (error, fallback) =>
	error?.ErrorMessage || error?.errorMessage || error?.Message || error?.message || fallback
const pickFirstDefined = (...values) => values.find((item) => item !== undefined && item !== null && item !== '')
const compactPayload = (payload = {}) =>
	Object.keys(payload).reduce((result, key) => {
		if (payload[key] !== undefined) {
			result[key] = payload[key]
		}
		return result
	}, {})
const parseIdValue = (value) => {
	if (value === undefined || value === null || value === '') {
		return undefined
	}
	return String(value)
}
const getSavedEntityId = (resData = {}, keys = []) => {
	if (typeof resData !== 'object' || Array.isArray(resData)) {
		return parseIdValue(resData)
	}
	return parseIdValue(pickFirstDefined(...keys.map((key) => resData[key]), resData.id, resData.Id))
}
const normalizeDateText = (value, options = {}) => {
	const { includeSeconds = false } = options
	if (!value) {
		return ''
	}
	const date = new Date(value)
	if (Number.isNaN(date.getTime())) {
		return String(value)
	}
	const pad = (num) => String(num).padStart(2, '0')
	const baseText = `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())} ${pad(
		date.getHours()
	)}:${pad(date.getMinutes())}`
	if (!includeSeconds) {
		return baseText
	}
	return `${baseText}:${pad(date.getSeconds())}`
}
const normalizePublishStatus = (status) => {
	if (status === 1 || status === '1' || status === true) {
		return '已发布'
	}
	return '未发布'
}
const normalizeActivityItem = (item = {}) => ({
	id: String(pickFirstDefined(item.ActivityId, item.activityId, '')),
	name: item.ActivityName ?? item.activityName ?? '',
	desc: item.ActivityDesc ?? item.activityDesc ?? '',
	coverImage: item.CoverImage ?? item.coverImage ?? '',
	background: item.CustomerBackground ?? item.customerBackground ?? '',
	start: normalizeDateText(item.StartTime ?? item.startTime),
	end: normalizeDateText(item.EndTime ?? item.endTime),
	duration: item.RecommendedDurationMinutes ?? item.recommendedDurationMinutes ?? 30,
	publishStatus: item.PublishStatus ?? item.publishStatus ?? 0,
	status: normalizePublishStatus(item.PublishStatus ?? item.publishStatus),
})
const normalizeStatistics = (data = {}) => ({
	totalCount: data.TotalCount ?? data.totalCount ?? 0,
	publishedCount: data.PublishedCount ?? data.publishedCount ?? 0,
	unpublishedCount: data.UnpublishedCount ?? data.unpublishedCount ?? 0,
	currentMonthNewCount: data.CurrentMonthNewCount ?? data.currentMonthNewCount ?? 0,
})
const normalizeRoleFromDetail = (item = {}, index = 0) => ({
	id: String(pickFirstDefined(item.roleId, item.RoleId, `role-${index + 1}-${Date.now()}`)),
	name: pickFirstDefined(item.roleNickname, item.RoleNickname, `角色${index + 1}`),
	persona: [pickFirstDefined(item.jobTitle, item.JobTitle), pickFirstDefined(item.projectRole, item.ProjectRole)]
		.filter(Boolean)
		.join(' / '),
	goal: pickFirstDefined(item.projectRequirement, item.ProjectRequirement, ''),
	desc: [pickFirstDefined(item.personality, item.Personality), pickFirstDefined(item.communicationStyle, item.CommunicationStyle)]
		.filter(Boolean)
		.join('；'),
	roleId: String(pickFirstDefined(item.roleId, item.RoleId, '')),
	roleNickname: pickFirstDefined(item.roleNickname, item.RoleNickname, ''),
	jobTitle: pickFirstDefined(item.jobTitle, item.JobTitle, ''),
	projectRole: pickFirstDefined(item.projectRole, item.ProjectRole, ''),
	personality: pickFirstDefined(item.personality, item.Personality, ''),
	communicationStyle: pickFirstDefined(item.communicationStyle, item.CommunicationStyle, ''),
	projectRequirement: pickFirstDefined(item.projectRequirement, item.ProjectRequirement, ''),
})
const getStageId = (item = {}, fallback = '') => String(pickFirstDefined(item.stageId, item.StageId, fallback))
const normalizeSceneFromDetail = (item = {}, fallback = {}) => ({
	id: String(getStageId(item, getStageId(fallback, `scene-${Date.now()}`))),
	name: pickFirstDefined(item.stageName, item.StageName, ''),
	objective: pickFirstDefined(item.stageTask, item.StageTask, ''),
	desc: [
		pickFirstDefined(item.stageDesc, item.StageDesc, ''),
		pickFirstDefined(item.questionCount, item.QuestionCount, 0) ? `建议题目数：${pickFirstDefined(item.questionCount, item.QuestionCount, 0)}` : '',
	]
		.filter(Boolean)
		.join('；'),
	stageId: String(getStageId(item, getStageId(fallback, ''))),
	stageName: pickFirstDefined(item.stageName, item.StageName, ''),
	stageDesc: pickFirstDefined(item.stageDesc, item.StageDesc, ''),
	stageTask: pickFirstDefined(item.stageTask, item.StageTask, ''),
	sortNo: pickFirstDefined(item.sortNo, item.SortNo, 0),
	questionCount: pickFirstDefined(item.questionCount, item.QuestionCount, 0),
})
const normalizeQuestionFromDetail = (item = {}, index = 0) => ({
	id: String(pickFirstDefined(item.questionId, item.QuestionId, `question-${index + 1}-${Date.now()}`)),
	questionId: String(pickFirstDefined(item.questionId, item.QuestionId, '')),
	content: pickFirstDefined(item.questionStem, item.QuestionStem, ''),
	assessmentPoints: pickFirstDefined(item.assessmentPoints, item.AssessmentPoints, ''),
	isRequired: Boolean(pickFirstDefined(item.isRequired, item.IsRequired, false)),
	sceneId: String(pickFirstDefined(item.stageId, item.StageId, '')),
	stageId: String(pickFirstDefined(item.stageId, item.StageId, '')),
	roleId: String(pickFirstDefined(item.roleId, item.RoleId, '')),
})
const buildDraftBaseFromDetail = (detail = {}, currentForm = {}) => {
	const activity = pickFirstDefined(detail.activity, detail.Activity, {})
	const roles = (pickFirstDefined(detail.roles, detail.Roles, []) || []).map((item, index) =>
		normalizeRoleFromDetail(item, index)
	)
	return {
		form: {
			name: pickFirstDefined(activity.activityName, activity.ActivityName, currentForm.name, ''),
			desc: pickFirstDefined(activity.activityDesc, activity.ActivityDesc, currentForm.desc, ''),
			coverImage: pickFirstDefined(activity.coverImage, activity.CoverImage, currentForm.coverImage, ''),
			background: pickFirstDefined(
				activity.customerBackground,
				activity.CustomerBackground,
				currentForm.background,
				''
			),
			time: [
				normalizeDateText(pickFirstDefined(activity.startTime, activity.StartTime, ''), {
					includeSeconds: true,
				}),
				normalizeDateText(pickFirstDefined(activity.endTime, activity.EndTime, ''), {
					includeSeconds: true,
				}),
			].filter(Boolean),
			duration: pickFirstDefined(
				activity.recommendedDurationMinutes,
				activity.RecommendedDurationMinutes,
				currentForm.duration,
				30
			),
			published: Number(pickFirstDefined(activity.publishStatus, activity.PublishStatus, 0)) === 1,
			backgroundAttachments: Array.isArray(currentForm.backgroundAttachments)
				? currentForm.backgroundAttachments.slice()
				: [],
			questionAttachments: Array.isArray(currentForm.questionAttachments)
				? currentForm.questionAttachments.slice()
				: [],
		},
		roles,
	}
}
const buildScenesFromSources = (stageList = [], detailStageList = []) => {
	const normalizedDetailStageList = Array.isArray(detailStageList) ? detailStageList : []
	const normalizedStageList = Array.isArray(stageList) && stageList.length ? stageList : normalizedDetailStageList
	const detailStageMap = new Map(
		normalizedDetailStageList.map((item) => [String(getStageId(item)), item]).filter(([stageId]) => Boolean(stageId))
	)

	return normalizedStageList
		.slice()
		.sort(
			(prev, next) =>
				pickFirstDefined(prev.sortNo, prev.SortNo, 0) - pickFirstDefined(next.sortNo, next.SortNo, 0)
		)
		.map((item) => {
			const stageId = String(getStageId(item))
			return normalizeSceneFromDetail(item, detailStageMap.get(stageId) || {})
		})
}
const buildQuestionsFromPage = (data = {}, fallbackItems = []) => {
	const items = pickFirstDefined(data.items, data.Items, fallbackItems, []) || []
	return items.map((item, index) => normalizeQuestionFromDetail(item, index))
}
const getExtractStatus = (resultData = {}) => String(resultData.Status || '').toLowerCase()
const getExtractResult = (resultData = {}) => resultData.Result || {}
const getExtractActivityId = (resultData = {}) => String(getExtractResult(resultData).ActivityId || '')
const createIntervalPolling = (poller, interval = POLL_INTERVAL) =>
	new Promise((resolve, reject) => {
		let timerId = 0
		let isDisposed = false
		let isPending = false

		const dispose = () => {
			isDisposed = true
			if (timerId) {
				window.clearInterval(timerId)
				timerId = 0
			}
		}

		const run = async () => {
			if (isDisposed || isPending) {
				return
			}
			isPending = true
			try {
				const result = await poller()
				if (!isDisposed && result !== undefined) {
					dispose()
					resolve(result)
				}
			} catch (error) {
				dispose()
				reject(error)
			} finally {
				isPending = false
			}
		}

		timerId = window.setInterval(run, interval)
		run()
	})
export default {
	name: 'ActivityManagementPage',
	components: {
		ActivityBasicInfoForm,
		ActivityConfigForm,
		ActivitySearchBar,
		ActivityStats,
		ActivityTable,
		QuestionManager,
		RoleManager,
		SceneManager,
	},
	data() {
			return {
				currentPage: 'list',
				editMode: false,
				activeStep: 0,
				editingId: null,
				pageLoading: false,
				statisticsLoading: false,
				uploading: false,
				aiGenerating: false,
				statistics: normalizeStatistics(),
				searchForm: {
					keyword: '',
					dateRange: [],
					status: '',
				},
				activities: [],
				draft: createEmptyDraft(),
				dimensions: dimensionOptions,
			}
		},
	computed: {
		stats() {
			return [
				{ label: '活动总数', value: this.statistics.totalCount, type: 'primary' },
				{ label: '已发布', value: this.statistics.publishedCount, type: 'success' },
				{ label: '未发布', value: this.statistics.unpublishedCount, type: '' },
				{ label: '本月新增', value: this.statistics.currentMonthNewCount, type: 'primary' },
			]
		},
		filteredActivities() {
			return this.activities.filter((item) => {
				const keyword = this.searchForm.keyword
				const matchKeyword =
					!keyword ||
					item.name.indexOf(keyword) !== -1 ||
					String(item.desc || '').indexOf(keyword) !== -1
				const matchStatus = !this.searchForm.status || item.status === this.searchForm.status
				const range = this.searchForm.dateRange || []
				const matchDate =
					!range.length ||
					(formatDateValue(item.start) >= formatDateValue(range[0]) &&
						formatDateValue(item.end) <= formatDateValue(range[1]))
				return matchKeyword && matchStatus && matchDate
			})
		},
		minStep() {
			return this.editMode ? 1 : 0
		},
		displayActiveStep() {
			return this.editMode ? Math.max(this.activeStep - 1, 0) : this.activeStep
		},
		createPageTitle() {
			return this.editMode ? '编辑路演活动' : '新增路演活动'
		},
		createPageSubtitle() {
			return this.editMode
				? '按活动配置、角色配置、业务环节配置、题库配置四步完成编辑。'
				: '按基础资料、活动配置、角色配置、业务环节配置、题库配置五步完成配置。'
		},
	},
	created() {
		this.initializePage()
	},
	methods: {
		async initializePage() {
			await Promise.all([this.fetchActivityList(), this.fetchActivityStatistics()])
		},
		resolvedNextButtonText() {
			if (this.aiGenerating) {
				return 'AI生成中...'
			}
			if (this.activeStep === 0 && !this.editMode) {
				return 'AI一键生成'
			}
			return this.activeStep === LAST_STEP ? '完成' : '下一步'
		},
		getListQueryParams() {
			const params = {
				PageIndex: 1,
				PageSize: 100,
			}
			const [startDate, endDate] = this.searchForm.dateRange || []
			if (startDate) {
				params.StartDate = startDate
			}
			if (endDate) {
				params.EndDate = endDate
			}
			if (this.searchForm.status === '已发布') {
				params.PublishStatus = 1
			}
			if (this.searchForm.status === '未发布') {
				params.PublishStatus = 0
			}
			return params
		},
		getActivityQuestionQueryParams(activityId, extraParams = {}) {
			return {
				ActivityId: activityId,
				PageIndex: 1,
				PageSize: DEFAULT_ACTIVITY_QUESTION_PAGE_SIZE,
				...extraParams,
			}
		},
		async fetchActivityQuestions(activityId, extraParams = {}) {
			const res = await GetActivityQuestionPage(this.getActivityQuestionQueryParams(activityId, extraParams))
			return getResponseData(res)
		},
		async fetchActivityBusinessStages(activityId) {
			const res = await GetActivityBusinessStageList(activityId)
			return getResponseData(res)
		},
		async fetchActivityList() {
			this.pageLoading = true
			try {
				const res = await GetActivityList(this.getListQueryParams())
				const data = getResponseData(res)
				const items = data.Items || data.items || []
				this.activities = items.map((item) => normalizeActivityItem(item))
			} catch (error) {
				this.$message.error(getErrorMessage(error, '获取活动列表失败，请稍后重试'))
			} finally {
				this.pageLoading = false
			}
		},
		async fetchActivityStatistics() {
			this.statisticsLoading = true
			try {
				const res = await GetActivityStatistics()
				this.statistics = normalizeStatistics(getResponseData(res))
			} catch (error) {
				this.$message.error(getErrorMessage(error, '获取活动统计失败，请稍后重试'))
			} finally {
				this.statisticsLoading = false
			}
		},
		handleSearch() {
			this.fetchActivityList()
		},
		resetSearch() {
			this.searchForm = {
				keyword: '',
				dateRange: [],
				status: '',
			}
			this.fetchActivityList()
		},
		backToList() {
			this.currentPage = 'list'
		},
		getPersistedActivityId() {
			return parseIdValue(this.editingId)
		},
		buildActivityUpdatePayload(activityId) {
			const form = this.draft.form || {}
			const [startTime = '', endTime = ''] = form.time || []
			return compactPayload({
				activityId,
				activityName: form.name || '',
				activityDesc: form.desc || '',
				coverImage: form.coverImage || '',
				coreGoal: '',
				customerBackground: form.background || '',
				startTime: startTime || undefined,
				endTime: endTime || undefined,
				recommendedDurationMinutes: form.duration === '' ? undefined : Number(form.duration),
				publishStatus: form.published ? 1 : 0,
			})
		},
		buildRolePayload(role = {}, activityId) {
			return compactPayload({
				activityId,
				roleNickname: pickFirstDefined(role.roleNickname, role.name, ''),
				jobTitle: pickFirstDefined(role.jobTitle, role.persona, ''),
				projectRole: pickFirstDefined(role.projectRole, ''),
				personality: pickFirstDefined(role.personality, ''),
				communicationStyle: pickFirstDefined(role.communicationStyle, role.desc, ''),
				projectRequirement: pickFirstDefined(role.projectRequirement, role.goal, ''),
			})
		},
		buildBusinessStagePayload(scene = {}, activityId, index = 0) {
			return compactPayload({
				activityId,
				stageName: pickFirstDefined(scene.stageName, scene.name, ''),
				stageDesc: pickFirstDefined(scene.stageDesc, scene.desc, ''),
				stageTask: pickFirstDefined(scene.stageTask, scene.objective, ''),
				sortNo: pickFirstDefined(scene.sortNo, scene.SortNo, index + 1),
			})
		},
		buildQuestionPayload(question = {}, stageId, activityId) {
			return compactPayload({
				activityId,
				roleId: parseIdValue(question.roleId),
				stageId,
				questionStem: pickFirstDefined(question.questionStem, question.content, ''),
				assessmentPoints: pickFirstDefined(question.assessmentPoints, ''),
				isRequired: Boolean(pickFirstDefined(question.isRequired, question.IsRequired, false)),
			})
		},
		ensureActivityId() {
			const activityId = this.getPersistedActivityId()
			if (!activityId) {
				this.$message.warning('当前活动尚未生成有效的活动ID，请先完成 AI 一键生成')
				return ''
			}
			return activityId
		},
		async updateActivityBase() {
			const activityId = this.ensureActivityId()
			if (!activityId) {
				return false
			}
			await UpdateActivity(this.buildActivityUpdatePayload(activityId))
			return true
		},
		async handleRoleSave(payload = {}) {
			const { mode, form = {}, onSuccess, onError } = payload
			const activityId = this.ensureActivityId()
			if (!activityId) {
				onError && onError()
				return
			}
			try {
				let savedRole = Object.assign({}, form)
				const roleId = parseIdValue(pickFirstDefined(form.roleId, form.RoleId, form.id))
				if (mode === 'edit' && roleId) {
					await UpdateRole({
						roleId,
						...this.buildRolePayload(form),
					})
					savedRole = Object.assign({}, form, {
						id: String(roleId),
						roleId: String(roleId),
					})
				} else {
					const saveRes = await SaveRole(this.buildRolePayload(form, activityId))
					const savedRoleId = getSavedEntityId(getResponseData(saveRes), ['roleId', 'RoleId'])
					savedRole = Object.assign({}, form, {
						id: String(savedRoleId || form.id),
						roleId: String(savedRoleId || form.roleId || ''),
					})
				}
				onSuccess && onSuccess(savedRole)
				this.$message.success(mode === 'edit' ? '角色已更新' : '角色已新增')
			} catch (error) {
				onError && onError()
				this.$message.error(getErrorMessage(error, mode === 'edit' ? '更新角色失败，请稍后重试' : '新增角色失败，请稍后重试'))
			}
		},
		async handleSceneSave(payload = {}) {
			const { mode, form = {}, onSuccess, onError } = payload
			const activityId = this.ensureActivityId()
			if (!activityId) {
				onError && onError()
				return
			}
			try {
				let savedScene = Object.assign({}, form)
				const stageId = parseIdValue(pickFirstDefined(form.stageId, form.StageId, form.id))
				if (mode === 'edit' && stageId) {
					await UpdateBusinessStage({
						stageId,
						...this.buildBusinessStagePayload(form),
					})
					savedScene = Object.assign({}, form, {
						id: String(stageId),
						stageId: String(stageId),
					})
				} else {
					const saveRes = await SaveBusinessStage(
						this.buildBusinessStagePayload(form, activityId, this.draft.scenes.length)
					)
					const savedStageId = getSavedEntityId(getResponseData(saveRes), ['stageId', 'StageId'])
					savedScene = Object.assign({}, form, {
						id: String(savedStageId || form.id),
						stageId: String(savedStageId || form.stageId || ''),
					})
				}
				onSuccess && onSuccess(savedScene)
				this.$message.success(mode === 'edit' ? '业务环节已更新' : '业务环节已新增')
			} catch (error) {
				onError && onError()
				this.$message.error(
					getErrorMessage(error, mode === 'edit' ? '更新业务环节失败，请稍后重试' : '新增业务环节失败，请稍后重试')
				)
			}
		},
		async handleQuestionSave(payload = {}) {
			const { mode, form = {}, onSuccess, onError } = payload
			const activityId = this.ensureActivityId()
			if (!activityId) {
				onError && onError()
				return
			}
			try {
				const stageId = parseIdValue(pickFirstDefined(form.stageId, form.sceneId))
				let savedQuestion = Object.assign({}, form, {
					sceneId: String(stageId || form.sceneId || ''),
					stageId: String(stageId || form.stageId || ''),
				})
				const questionId = parseIdValue(pickFirstDefined(form.questionId, form.QuestionId, form.id))
				if (mode === 'edit' && questionId) {
					await UpdateQuestion({
						questionId,
						...this.buildQuestionPayload(form, stageId),
					})
					savedQuestion = Object.assign({}, savedQuestion, {
						id: String(questionId),
						questionId: String(questionId),
					})
				} else {
					const saveRes = await SaveQuestion(this.buildQuestionPayload(form, stageId, activityId))
					const savedQuestionId = getSavedEntityId(getResponseData(saveRes), ['questionId', 'QuestionId'])
					savedQuestion = Object.assign({}, savedQuestion, {
						id: String(savedQuestionId || form.id),
						questionId: String(savedQuestionId || form.questionId || ''),
					})
				}
				onSuccess && onSuccess(savedQuestion)
				this.$message.success(mode === 'edit' ? '题目已更新' : '题目已新增')
			} catch (error) {
				onError && onError()
				this.$message.error(getErrorMessage(error, mode === 'edit' ? '更新题目失败，请稍后重试' : '新增题目失败，请稍后重试'))
			}
		},
		async handleRoleDelete(payload = {}) {
			const { row = {}, onSuccess, onError } = payload
			const roleId = parseIdValue(pickFirstDefined(row.roleId, row.RoleId, row.id))
			if (!roleId) {
				onError && onError()
				this.$message.warning('未获取到可删除的角色ID')
				return
			}
			try {
				await RemoveRole({ roleId })
				onSuccess && onSuccess()
				this.$message.success('角色已删除')
			} catch (error) {
				onError && onError()
				this.$message.error(getErrorMessage(error, '删除角色失败，请稍后重试'))
			}
		},
		async handleSceneDelete(payload = {}) {
			const { row = {}, onSuccess, onError } = payload
			const stageId = parseIdValue(pickFirstDefined(row.stageId, row.StageId, row.id))
			if (!stageId) {
				onError && onError()
				this.$message.warning('未获取到可删除的业务环节ID')
				return
			}
			try {
				await RemoveBusinessStage({ stageId })
				onSuccess && onSuccess()
				this.$message.success('业务环节已删除')
			} catch (error) {
				onError && onError()
				this.$message.error(getErrorMessage(error, '删除业务环节失败，请稍后重试'))
			}
		},
		async handleQuestionDelete(payload = {}) {
			const { row = {}, onSuccess, onError } = payload
			const questionId = parseIdValue(pickFirstDefined(row.questionId, row.QuestionId, row.id))
			if (!questionId) {
				onError && onError()
				this.$message.warning('未获取到可删除的题目ID')
				return
			}
			try {
				await RemoveQuestion({ questionId })
				onSuccess && onSuccess()
				this.$message.success('题目已删除')
			} catch (error) {
				onError && onError()
				this.$message.error(getErrorMessage(error, '删除题目失败，请稍后重试'))
			}
		},
		async handleQuestionBatchDelete(payload = {}) {
			const { rows = [], onSuccess, onError } = payload
			const questionIds = rows
				.map((item) => parseIdValue(pickFirstDefined(item.questionId, item.QuestionId, item.id)))
				.filter(Boolean)
			if (!questionIds.length) {
				onError && onError()
				this.$message.warning('请选择需要删除的题目')
				return
			}
			try {
				await BatchRemoveQuestion({
					questionIds,
				})
				onSuccess && onSuccess(questionIds)
				this.$message.success('题目已批量删除')
			} catch (error) {
				onError && onError()
				this.$message.error(getErrorMessage(error, '批量删除题目失败，请稍后重试'))
			}
		},
		updateAttachments(payload) {
			const { materialType, attachments } = payload
			if (materialType === MATERIAL_TYPE_QUESTION) {
				this.draft.form.questionAttachments = attachments
				return
			}
			this.draft.form.backgroundAttachments = attachments
		},
		updateCoverImage(coverImage) {
			this.draft.form.coverImage = coverImage || ''
		},
		parseUploadResponse(responseText = '') {
			if (!responseText) {
				return {}
			}
			try {
				return JSON.parse(responseText)
			} catch (error) {
				return { fileId: responseText }
			}
		},
		getUploadedFileValue(result = {}) {
			return (
				result?.fileId ||
				result?.FileId ||
				result?.data?.fileId ||
				result?.Data?.fileId ||
				result?.Data?.FileId ||
				result?.referer ||
				result?.Referer ||
				''
			)
		},
		async uploadActivityAsset(file, type = 'uploadfile') {
			const formData = new FormData()
			formData.append('file', file)
			const response = await fetch(
				`${process.env.VUE_APP_BASEURL}/Beta.FileServiceV2/Api/UserFile?modelID=61015&type=${type}&Storage=aicoach`,
				{
					method: 'POST',
					body: formData,
					credentials: 'include',
				}
			)
			if (!response.ok) {
				throw new Error(type === 'uploadimage' ? '图片上传失败' : '文件上传失败')
			}
			const result = this.parseUploadResponse(await response.text())
			const uploadedValue = this.getUploadedFileValue(result)
			if (!uploadedValue) {
				throw new Error(type === 'uploadimage' ? '上传成功，但未返回图片标识' : '上传成功，但未返回文件标识')
			}
			return {
				result,
				uploadedValue,
			}
		},
		async handleUploadFile(options) {
			const { file, onSuccess, onError, materialType } = options
			this.uploading = true
			try {
				const { result, uploadedValue } = await this.uploadActivityAsset(file, 'uploadfile')
				const sourceList =
					materialType === MATERIAL_TYPE_QUESTION
						? this.draft.form.questionAttachments || []
						: this.draft.form.backgroundAttachments || []
				const nextAttachments = sourceList.concat([
					{
						name: file.name,
						size: file.size,
						uid: file.uid,
						fileId: uploadedValue,
					},
				])
				this.updateAttachments({
					materialType,
					attachments: nextAttachments,
				})
				onSuccess && onSuccess(result)
				this.$message.success(`文件“${file.name}”上传成功`)
			} catch (error) {
				onError && onError(error)
				this.$message.error(getErrorMessage(error, `文件“${file.name}”上传失败`))
			} finally {
				this.uploading = false
			}
		},
		async handleUploadCoverImage(options) {
			const { file, onSuccess, onError } = options
			this.uploading = true
			try {
				const { result, uploadedValue } = await this.uploadActivityAsset(file, 'uploadimage')
				this.updateCoverImage(uploadedValue)
				onSuccess && onSuccess(result)
				this.$message.success(`图片“${file.name}”上传成功`)
			} catch (error) {
				onError && onError(error)
				this.$message.error(getErrorMessage(error, `图片“${file.name}”上传失败`))
			} finally {
				this.uploading = false
			}
		},
		newActivity() {
			this.editMode = false
			this.activeStep = 0
			this.editingId = null
			this.draft = {
				form: createDefaultActivityForm(),
				roles: [],
				scenes: [],
				questions: [],
			}
			this.currentPage = 'create'
		},
		async editActivity(row) {
			this.editMode = true
			this.activeStep = 1
			this.editingId = row.id
			this.currentPage = 'create'
			await this.loadActivityDetail(row.id, {
				preserveAttachments: false,
				fallbackRow: row,
			})
		},
		async loadActivityDetail(activityId, options = {}) {
			const { preserveAttachments = true, fallbackRow = null } = options
			this.pageLoading = true
			try {
				const detailRes = await GetActivityDetail(activityId)
				const detail = getResponseData(detailRes)
				const detailBusinessStages = pickFirstDefined(detail.businessStages, detail.BusinessStages, []) || []
				const detailQuestions = pickFirstDefined(detail.questions, detail.Questions, []) || []
				const [stageResult, questionResult] = await Promise.allSettled([
					this.fetchActivityBusinessStages(activityId),
					this.fetchActivityQuestions(activityId),
				])
				const currentForm = preserveAttachments
					? this.draft.form
					: {
							...createDraftFromActivity(fallbackRow || {}).form,
							backgroundAttachments: [],
							questionAttachments: [],
						}
				const nextDraft = buildDraftBaseFromDetail(detail, currentForm)
				const stageList = stageResult.status === 'fulfilled' ? stageResult.value : detailBusinessStages
				const questionPage = questionResult.status === 'fulfilled' ? questionResult.value : {}
				nextDraft.scenes = buildScenesFromSources(stageList, detailBusinessStages)
				nextDraft.questions = buildQuestionsFromPage(questionPage, detailQuestions)
				this.draft = nextDraft
				if (fallbackRow) {
					fallbackRow.config = cloneDraft(this.draft)
				}
				const warnings = []
				if (stageResult.status === 'rejected') {
					warnings.push('业务环节')
				}
				if (questionResult.status === 'rejected') {
					warnings.push('题库')
				}
				if (warnings.length) {
					this.$message.warning(`${warnings.join('、')}加载失败，已使用详情接口中的兼容数据`)
				}
			} catch (error) {
				if (fallbackRow) {
					this.draft = cloneDraft(fallbackRow.config || createDraftFromActivity(fallbackRow))
				}
				this.$message.error(getErrorMessage(error, '获取活动详情失败，请稍后重试'))
			} finally {
				this.pageLoading = false
			}
		},
		handleDeleteActivityRemote(row) {
			this.$confirm(`确认删除活动“${row.name}”吗？`, '提示', {
				type: 'warning',
			})
				.then(async () => {
					this.pageLoading = true
					try {
						await RemoveActivity({
							activityId: parseIdValue(pickFirstDefined(row.activityId, row.ActivityId, row.id)),
						})
						await Promise.all([this.fetchActivityList(), this.fetchActivityStatistics()])
						this.$message.success('活动已删除')
					} catch (error) {
						this.$message.error(getErrorMessage(error, '删除活动失败，请稍后重试'))
					} finally {
						this.pageLoading = false
					}
				})
				.catch(() => {})
		},
		previousStep() {
			if (this.activeStep > this.minStep) {
				this.activeStep -= 1
			}
		},
		async nextStep() {
			if (this.activeStep === 0 && !this.editMode) {
				if (!this.validateBasicMaterials()) {
					return
				}
				await this.runExtractGenerateWithSeparateFiles()
				return
			}

			if (!this.validateCurrentStep()) {
				return
			}

			if (this.activeStep === 1) {
				this.pageLoading = true
				try {
					const canContinue = await this.updateActivityBase()
					if (!canContinue) {
						return
					}
				} catch (error) {
					this.$message.error(getErrorMessage(error, '活动配置保存失败，请稍后重试'))
					return
				} finally {
					this.pageLoading = false
				}
			}

			if (this.activeStep < LAST_STEP) {
				this.activeStep += 1
				return
			}

			await this.saveActivity()
		},
		validateCurrentStep() {
			if (this.activeStep === 1) {
				const form = this.draft.form
				if (!form.name || !form.desc || !form.background || !form.time.length || !form.duration) {
					this.$message.warning('请完整填写活动配置')
					return false
				}
			}
			if (this.activeStep === 2 && !this.draft.roles.length) {
				this.$message.warning('请至少配置一个角色')
				return false
			}
			if (this.activeStep === 3 && !this.draft.scenes.length) {
				this.$message.warning('请至少配置一个业务环节')
				return false
			}
			if (this.activeStep === 4 && !this.draft.questions.length) {
				this.$message.warning('请至少配置一道题目')
				return false
			}
			return true
		},
		validateBasicMaterials() {
			if (!(this.draft.form.backgroundAttachments || []).length) {
				this.$message.warning('请先上传活动背景资料')
				return false
			}
			if (!(this.draft.form.questionAttachments || []).length) {
				this.$message.warning('请先上传活动题库资料')
				return false
			}
			if (this.uploading) {
				this.$message.warning('文件还在上传中，请稍候')
				return false
			}
			return true
		},
		async runExtractGenerateWithSeparateFiles() {
			this.aiGenerating = true
			const loading = this.$loading({
				lock: true,
				text: 'AI 一键生成中...',
				spinner: 'el-icon-loading',
				background: 'rgba(255, 255, 255, 0.86)',
			})
			try {
				const backgroundFiles = (this.draft.form.backgroundAttachments || []).map((item) => item.fileId).filter(Boolean)
				const questionFiles = (this.draft.form.questionAttachments || []).map((item) => item.fileId).filter(Boolean)
				if (!backgroundFiles.length) {
					throw new Error('未获取到可用的活动背景资料文件标识，请重新上传')
				}
				if (!questionFiles.length) {
					throw new Error('未获取到可用的活动题库资料文件标识，请重新上传')
				}

				const activitySubmitRes = await ExtractActivityInfo({ files: backgroundFiles })
				const activitySubmitData = getResponseData(activitySubmitRes)
				const activityTaskId = activitySubmitData.TaskId || activitySubmitData.taskId
				if (!activityTaskId) {
					throw new Error('未获取到活动资料提取任务编号')
				}

				const activityId = await this.pollExtractActivityResult(activityTaskId)
				const questionSubmitRes = await ExtractQuestionInfo({
					activityId,
					files: questionFiles,
				})
				const questionSubmitData = getResponseData(questionSubmitRes)
				const questionTaskId = questionSubmitData.TaskId || questionSubmitData.taskId
				if (!questionTaskId) {
					throw new Error('未获取到题库资料提取任务编号')
				}

				await this.pollExtractQuestionResult(questionTaskId, activityId)
				this.editingId = activityId
				await this.loadActivityDetail(activityId, {
					preserveAttachments: true,
				})
				this.activeStep = 1
				this.$message.success('AI 已完成活动资料和题库资料提取，请继续完善活动配置')
			} catch (error) {
				this.$message.error(getErrorMessage(error, 'AI 一键生成失败，请稍后重试'))
			} finally {
				this.aiGenerating = false
				loading.close()
			}
		},
		async pollExtractActivityResult(taskId) {
			return createIntervalPolling(async () => {
				const resultRes = await GetExtractActivityInfoResult(taskId)
				const resultData = getResponseData(resultRes)
				const status = getExtractStatus(resultData)
				if (status === 'success') {
					const activityId = getExtractActivityId(resultData)
					if (!activityId) {
						throw new Error('AI 生成成功，但未返回活动ID')
					}
					return activityId
				}
				if (status === 'failed') {
					throw new Error(resultData.ErrorMessage || '活动信息提取失败')
				}
				return undefined
			})
		},
		async pollExtractQuestionResult(taskId, activityId) {
			return createIntervalPolling(async () => {
				const resultRes = await GetExtractQuestionInfoResult({
					taskId,
					activityId,
				})
				const resultData = getResponseData(resultRes)
				const status = getExtractStatus(resultData)
				if (status === 'success') {
					const extractedActivityId = getExtractActivityId(resultData)
					if (!extractedActivityId) {
						throw new Error('题库信息提取成功，但未返回活动ID')
					}
					return true
				}
				if (status === 'failed') {
					throw new Error(resultData.ErrorMessage || '题库信息提取失败')
				}
				return undefined
			})
		},
		async saveActivity() {
			const activityId = this.ensureActivityId()
			if (!activityId) {
				this.$message.warning('当前活动尚未生成有效的活动ID，请先完成 AI 一键生成')
				return
			}

			this.pageLoading = true
			try {
				await Promise.all([this.fetchActivityList(), this.fetchActivityStatistics()])
				this.currentPage = 'list'
				this.$message.success('活动配置已保存')
			} catch (error) {
				this.$message.error(getErrorMessage(error, '活动配置保存失败，请稍后重试'))
			} finally {
				this.pageLoading = false
			}
		},
	},
}
</script>

<style scoped>
.activity-management-page {
	min-height: 100vh;
	padding: 28px;
	background:
		radial-gradient(circle at top left, rgba(64, 158, 255, 0.12), transparent 24%),
		linear-gradient(180deg, #f6f9fc 0%, #eef3f9 100%);
}

.loading-mask-host {
	min-height: calc(100vh - 56px);
}

.page-section {
	width: 100%;
}

.page-header {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 16px;
	margin-bottom: 20px;
}

.page-title {
	font-size: 28px;
	font-weight: 700;
	color: #1f2d3d;
}

.page-subtitle {
	margin-top: 8px;
	font-size: 14px;
	color: #6b778c;
}

.page-card {
	padding: 24px;
	border-radius: 24px;
	background: rgba(255, 255, 255, 0.96);
	box-shadow: 0 18px 40px rgba(31, 45, 61, 0.08);
}

.stepper {
	margin-bottom: 34px;
}

.footer-actions {
	display: flex;
	justify-content: flex-end;
	gap: 10px;
	margin-top: 28px;
	padding-top: 20px;
	border-top: 1px solid #edf2f7;
}

@media (max-width: 768px) {
	.activity-management-page {
		padding: 16px;
	}

	.page-header {
		flex-direction: column;
	}

	.page-card {
		padding: 16px;
		border-radius: 18px;
	}
}
</style>
