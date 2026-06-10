<template>
	<div class="activity-brief-page" :class="[isPc ? 'pc_page_container' : '']">
		<div class="activity-brief-main" :class="[isPc ? 'pc_container' : '']">
			<section v-if="loading" class="brief-state-card">
				<div class="brief-state-title">活动信息加载中</div>
				<p class="brief-state-desc">正在获取演练任务书详情，请稍候。</p>
			</section>

			<roadshow-empty
				v-else-if="loaded && !activity.activityId"
				title="未找到活动详情"
				description="请返回活动大厅重新选择活动。"
			/>

			<section v-else class="mission-card">
				<div class="mission-cover">
					<div class="mission-badge">{{ statusText }}</div>
					<h1 class="mission-title">{{ activity.activityName || '演练任务书' }}</h1>
					<p class="page-subtitle">
						进入前请确认目标背景。交锋将随答题自动推进，不显化内部场景节点，还原突发沟通感。
					</p>
					<div class="mission-meta">
						<div class="mission-meta-item">
							<span>演练时长</span>
							<b>{{ durationText }}</b>
						</div>
						<div class="mission-meta-item">
							<span>活动周期</span>
							<b>{{ timeRangeText }}</b>
						</div>
					</div>
				</div>

				<div class="section-title">任务背景</div>
				<p class="mission-p">
					<strong>演练背景：</strong>{{ backgroundText }}
				</p>

				<div class="section-title">考核能力模型</div>
				<div class="cap-grid">
					<div v-for="item in abilityModelList" :key="item.abilityDimensionId || item.title" class="cap-item">
						<strong>{{ item.title }}：</strong>{{ item.desc }}
					</div>
					<div v-if="!abilityModelList.length" class="cap-item">暂无考核能力模型信息</div>
				</div>

				<div v-if="roles.length" class="section-title">关键角色画像</div>
				<div v-if="roles.length" class="role-list">
					<article v-for="role in roles" :key="role.roleId" class="role-card">
						<div class="role-card-head">
							<div>
								<h3>{{ role.roleNickname || '关键角色' }}</h3>
								<p>{{ role.jobTitle || role.projectRole || '角色信息待补充' }}</p>
							</div>
							<span class="role-pill">{{ role.projectRole || '项目角色' }}</span>
						</div>
						<div class="role-tags">
							<span v-if="role.personality" class="role-tag">{{ role.personality }}</span>
							<span v-if="role.communicationStyle" class="role-tag">{{ role.communicationStyle }}</span>
						</div>
						<p v-if="role.projectRequirement" class="role-requirement">{{ role.projectRequirement }}</p>
					</article>
				</div>

				<div class="action-bar">
					<div class="action-bar-inner" :class="[isPc ? 'pc_container' : '']">
						<button
							v-if="activity.activityId"
							type="button"
							class="primary-btn"
							:disabled="startingCombat"
							@click="goCombat"
						>
							进入实战交锋
						</button>
						<button type="button" class="ghost-btn" @click="goHome">返回活动大厅</button>
					</div>
				</div>
			</section>
		</div>
	</div>
</template>

<script>
import { computed, onMounted, reactive, ref, toRefs } from 'vue'
import { useRoute } from 'vue-router'
import { Toast } from 'vant'
import RoadshowEmpty from './components/roadshowEmpty.vue'
import { GetActivityDetail, GetAbilityDimensionListByCustCompanyId, StartPractice } from '@/api/activity'
import { utcToLocal } from '@/utils/util'

export default {
	name: 'ActivityBrief',
	components: {
		RoadshowEmpty,
	},
	setup() {
		const route = useRoute()
		const isPc = ref((window.bjsdk.isWinPC || window.bjsdk.isMac) && !window.bjsdk.isWM)
		const state = reactive({
			loading: false,
			loaded: false,
			startingCombat: false,
			activity: {},
			roles: [],
			abilityModelList: [],
		})

		const toText = (value) => {
			if (value === null || value === undefined) return ''
			return `${value}`.trim()
		}

		const toLocalText = (value) => {
			const text = utcToLocal(value)
			return text || ''
		}

		const formatDate = (value) => {
			const text = toLocalText(value)
			if (!text) return ''
			return text.slice(0, 10)
		}

		const formatDateRange = (start, end) => {
			const startText = formatDate(start)
			const endText = formatDate(end)
			if (startText && endText) return startText === endText ? startText : `${startText} - ${endText}`
			return startText || endText || '时间待定'
		}

		const normalizeActivity = (item = {}) => ({
			activityId: toText(item.activityId ?? item.ActivityId),
			custCompanyId: toText(item.custCompanyId ?? item.CustCompanyId),
			activityName: toText(item.activityName ?? item.ActivityName),
			activityDesc: toText(item.activityDesc ?? item.ActivityDesc),
			coverImage: toText(item.coverImage ?? item.CoverImage),
			coreGoal: toText(item.coreGoal ?? item.CoreGoal),
			customerBackground: toText(item.customerBackground ?? item.CustomerBackground),
			startTime: item.startTime ?? item.StartTime ?? '',
			endTime: item.endTime ?? item.EndTime ?? '',
			recommendedDurationMinutes: Number(item.recommendedDurationMinutes ?? item.RecommendedDurationMinutes ?? 0) || 0,
			publishStatus: Number(item.publishStatus ?? item.PublishStatus ?? 0) || 0,
		})

		const normalizeRole = (item = {}) => ({
			roleId: toText(item.roleId ?? item.RoleId),
			roleNickname: toText(item.roleNickname ?? item.RoleNickname),
			jobTitle: toText(item.jobTitle ?? item.JobTitle),
			projectRole: toText(item.projectRole ?? item.ProjectRole),
			personality: toText(item.personality ?? item.Personality),
			communicationStyle: toText(item.communicationStyle ?? item.CommunicationStyle),
			projectRequirement: toText(item.projectRequirement ?? item.ProjectRequirement),
		})

		const normalizeAbilityModel = (item = {}) => ({
			abilityDimensionId: toText(item.abilityDimensionId ?? item.AbilityDimensionId),
			title: toText(item.abilityDimensionName ?? item.AbilityDimensionName) || '未命名能力维度',
			desc: toText(item.abilityDimensionDesc ?? item.AbilityDimensionDesc) || '暂无能力维度说明',
		})

		const activityId = computed(() => {
			return toText(route.query.activityId || route.query.ActivityId || window.bjsdk.query('activityId') || window.bjsdk.query('ActivityId'))
		})

		const durationText = computed(() => {
			const duration = Number(state.activity.recommendedDurationMinutes || 0)
			return duration > 0 ? `${duration} 分钟` : '时长待定'
		})

		const timeRangeText = computed(() => formatDateRange(state.activity.startTime, state.activity.endTime))

		const statusText = computed(() => {
			if (!state.activity.activityId) return '任务书'
			const now = Date.now()
			const start = new Date(toLocalText(state.activity.startTime).replace(/-/g, '/')).getTime()
			const end = new Date(toLocalText(state.activity.endTime).replace(/-/g, '/')).getTime()
			if (Number.isFinite(end) && end > 0 && now > end) return '已结束'
			if (Number.isFinite(start) && start > 0 && now < start) return '即将开始'
			return '进行中'
		})

		const backgroundText = computed(() => {
			const pieces = [
				toText(state.activity.activityDesc),
				toText(state.activity.customerBackground),
			].filter(Boolean)
			return pieces.join(' ') || '暂无任务背景说明'
		})

		const loadAbilityModels = async (custCompanyId) => {
			const id = toText(custCompanyId)
			if (!id) {
				state.abilityModelList = []
				return
			}

			try {
				const res = await GetAbilityDimensionListByCustCompanyId({ custCompanyId: id })
				const data = res.data || res.Data || []
				const list = Array.isArray(data) ? data : []
				state.abilityModelList = list.map(normalizeAbilityModel)
			} catch (error) {
				state.abilityModelList = []
				const message = error && (error.errorMessage || error.ErrorMessage || error.message)
				Toast(message || '考核能力模型获取失败，请稍后重试')
			}
		}

		const loadDetail = async () => {
			if (!activityId.value) {
				state.loaded = true
				Toast('缺少活动ID，无法获取任务书详情')
				return
			}

			try {
				state.loading = true
				const res = await GetActivityDetail({ activityId: activityId.value })
				const data = res.data || res.Data || {}
				state.activity = normalizeActivity(data.activity || data.Activity || {})
				state.roles = (Array.isArray(data.roles) ? data.roles : (Array.isArray(data.Roles) ? data.Roles : []))
					.map(normalizeRole)
				await loadAbilityModels(state.activity.custCompanyId)
			} catch (error) {
				const message = error && (error.errorMessage || error.ErrorMessage || error.message)
				Toast(message || '任务书详情获取失败，请稍后重试')
			} finally {
				state.loading = false
				state.loaded = true
			}
		}

		const goHome = () => {
			window.bjsdk.goTo('/home', {})
		}

		const goCombat = async () => {
			const id = toText(state.activity.activityId)
			if (!id) {
				Toast('活动ID缺失，暂时无法进入实战交锋')
				return
			}
			if (state.startingCombat) return

			try {
				state.startingCombat = true
				const practiceResponse = await StartPractice({
					activityId: id,
					roadshowMaterialId: null,
				})
				const practiceData = practiceResponse.data || practiceResponse.Data || {}
				const practiceRecordId = toText(practiceData.practiceRecordId ?? practiceData.PracticeRecordId)
				if (!practiceRecordId) {
					throw new Error('创建演练记录失败，未返回有效记录 ID')
				}
				window.bjsdk.goTo('/activityCombat', {
					append: {
						activityId: id,
						practiceRecordId,
					},
				})
			} catch (error) {
				const message = error && (error.errorMessage || error.ErrorMessage || error.message)
				Toast.fail(message || '进入实战交锋失败，请稍后重试')
			} finally {
				state.startingCombat = false
			}
		}

		onMounted(() => {
			loadDetail()
		})

		return {
			isPc,
			durationText,
			timeRangeText,
			statusText,
			backgroundText,
			goCombat,
			goHome,
			...toRefs(state),
		}
	},
}
</script>

<style lang="less" scoped>
.activity-brief-page {
	min-height: 100vh;
	background:
		radial-gradient(circle at top right, rgba(96, 215, 255, 0.24), transparent 28%),
		linear-gradient(180deg, #f8fbff 0%, #eef3f8 42%, #edf2f7 100%);
	color: #0f172a;

	.activity-brief-main {
		padding: 14px 16px calc(96px + env(safe-area-inset-bottom));
	}

	.brief-state-card,
	.mission-card {
		padding: 18px;
		border-radius: 20px;
		background: #ffffff;
		border: 1px solid #e2e8f0;
		box-shadow: 0 10px 25px -5px rgba(15, 23, 42, 0.05);
	}

	.brief-state-card {
		margin-top: 0;
	}

	.brief-state-title {
		font-size: 16px;
		font-weight: 800;
		color: #0f172a;
	}

	.brief-state-desc {
		margin: 8px 0 0;
		font-size: 12px;
		line-height: 1.7;
		color: #64748b;
	}

	.mission-cover {
		position: relative;
		margin: -18px -18px 16px;
		padding: 20px;
		border-radius: 20px 20px 16px 16px;
		background: linear-gradient(135deg, #f8fcff 0%, #eaf5ff 100%);
		border-bottom: 1px solid rgba(64, 158, 255, 0.12);
	}

	.mission-badge {
		display: inline-flex;
		padding: 3px 8px;
		border-radius: 6px;
		background: #ecf5ff;
		border: 1px solid rgba(64, 158, 255, 0.18);
		color: #409eff;
		font-size: 11px;
		font-weight: 700;
	}

	.mission-title {
		margin: 12px 0 0;
		font-size: 18px;
		line-height: 1.35;
		font-weight: 800;
		color: #0f172a;
	}

	.page-subtitle {
		margin: 8px 0 0;
		font-size: 12px;
		line-height: 1.7;
		color: #64748b;
	}

	.mission-meta {
		display: grid;
		grid-template-columns: repeat(2, minmax(0, 1fr));
		gap: 10px;
		margin-top: 16px;
	}

	.mission-meta-item {
		padding: 12px;
		border-radius: 14px;
		background: rgba(255, 255, 255, 0.72);
		border: 1px solid rgba(64, 158, 255, 0.1);

		span {
			display: block;
			font-size: 11px;
			color: #64748b;
		}

		b {
			display: block;
			margin-top: 4px;
			font-size: 14px;
			font-weight: 800;
			color: #0f172a;
		}
	}

	.section-title {
		display: flex;
		align-items: center;
		gap: 8px;
		margin: 16px 0 8px;
		font-size: 14px;
		font-weight: 800;
		color: #0f172a;

		&::before {
			content: '';
			width: 3.5px;
			height: 14px;
			border-radius: 2px;
			background: #409eff;
		}
	}

	.mission-p {
		margin: 6px 0;
		font-size: 13px;
		line-height: 1.7;
		color: #475569;
		text-align: justify;
	}

	.cap-grid,
	.role-list {
		display: flex;
		flex-direction: column;
		gap: 10px;
	}

	.cap-item,
	.role-card {
		padding: 12px 14px;
		border-radius: 14px;
		background: #f8fafc;
		border: 1px solid #e2e8f0;
	}

	.cap-item {
		font-size: 12.5px;
		line-height: 1.6;
		color: #334155;
	}

	.role-card-head {
		display: flex;
		align-items: flex-start;
		justify-content: space-between;
		gap: 12px;
	}

	.role-card-head {
		h3 {
			margin: 0;
			font-size: 14px;
			font-weight: 800;
			color: #0f172a;
		}

		p {
			margin: 4px 0 0;
			font-size: 12px;
			line-height: 1.5;
			color: #64748b;
		}
	}

	.role-pill,
	.role-tag {
		display: inline-flex;
		align-items: center;
		border-radius: 999px;
		font-size: 11px;
		font-weight: 700;
	}

	.role-pill {
		flex-shrink: 0;
		padding: 5px 10px;
		background: #ecf5ff;
		color: #409eff;
	}

	.role-tags {
		display: flex;
		flex-wrap: wrap;
		gap: 8px;
		margin-top: 10px;
	}

	.role-tag {
		padding: 4px 10px;
		background: #ffffff;
		border: 1px solid #dbeafe;
		color: #2563eb;
	}

	.role-requirement {
		margin: 10px 0 0;
		font-size: 12.5px;
		line-height: 1.7;
		color: #475569;
	}

	.action-bar {
		position: fixed;
		left: 0;
		right: 0;
		bottom: 0;
		z-index: 30;
		padding: 12px 16px calc(12px + env(safe-area-inset-bottom));
		background: linear-gradient(180deg, rgba(248, 251, 255, 0) 0%, rgba(248, 251, 255, 0.92) 22%, rgba(248, 251, 255, 0.98) 100%);
		backdrop-filter: blur(10px);
	}

	.action-bar-inner {
		max-width: 100%;
	}

	.primary-btn,
	.ghost-btn {
		width: 100%;
		height: 44px;
		border-radius: 22px;
		font-size: 13px;
		font-weight: 700;
		cursor: pointer;
	}

	.primary-btn {
		margin-bottom: 10px;
		border: none;
		background: linear-gradient(135deg, #409eff, #2563eb);
		box-shadow: 0 10px 24px rgba(37, 99, 235, 0.2);
		color: #ffffff;
	}

	.ghost-btn {
		border: 1px solid #d6e3f5;
		background: rgba(255, 255, 255, 0.96);
		box-shadow: 0 10px 25px rgba(37, 99, 235, 0.08);
		color: #334155;
	}
}
</style>
