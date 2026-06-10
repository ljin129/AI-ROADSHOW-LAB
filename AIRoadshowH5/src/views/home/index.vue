<template>
	<div class="page-home" :class="[isPc ? 'pc_page_container' : '']">
		<div class="page-home-main" :class="[isPc ? 'pc_container' : '']" :style="{ paddingTop: contentTop }">
			<roadshow-hero />

			<section class="activity-section">
				<div class="section-head">
					<div>
						<h2>活动大厅</h2>
					</div>
					<span class="section-count">{{ totalText }}</span>
				</div>

				<van-pull-refresh v-model="refreshing" @refresh="onRefresh">
					<van-list
						v-model:loading="loading"
						:finished="finished"
						:immediate-check="false"
						:finished-text="activities.length ? '没有更多活动了' : ''"
						@load="onLoad"
					>
						<div class="activity-list">
							<roadshow-activity-card
								v-for="item in activities"
								:key="item.activityId"
								:activity="item"
								@click="onSelectActivity"
							/>
						</div>
					</van-list>
				</van-pull-refresh>

				<roadshow-empty
					v-if="loaded && !loading && !refreshing && activities.length === 0"
					title="暂无已发布活动"
					description="当前机构下还没有可参与的活动，稍后下拉刷新再试试。"
				/>
			</section>
		</div>
	</div>
</template>

<script>
import { computed, onMounted, reactive, ref, toRefs } from 'vue'
import { PullRefresh, List, Toast } from 'vant'
import { GetPublishedActivityList } from '@/api/activity'
import RoadshowHero from './components/roadshowHero.vue'
import RoadshowActivityCard from './components/roadshowActivityCard.vue'
import RoadshowEmpty from './components/roadshowEmpty.vue'

export default {
	name: 'Home',
	components: {
		RoadshowHero,
		RoadshowActivityCard,
		RoadshowEmpty,
		[PullRefresh.name]: PullRefresh,
		[List.name]: List,
	},
	setup() {
		const isPc = ref((window.bjsdk.isWinPC || window.bjsdk.isMac) && !window.bjsdk.isWM)
		const pageTitle = ref(document.title || 'AI路演智能演练舱')
		const state = reactive({
			activities: [],
			loading: false,
			refreshing: false,
			finished: false,
			loaded: false,
			pageIndex: 0,
			pageSize: 10,
			total: 0,
		})

		const contentTop = computed(() => '0px')
		const totalText = computed(() => `共 ${state.total || 0} 场`)

		const toText = (value) => {
			if (value === null || value === undefined) return ''
			return `${value}`.trim()
		}

		const getResponseData = (res) => res.data || res.Data || {}

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
			createTime: item.createTime ?? item.CreateTime ?? '',
			updateTime: item.updateTime ?? item.UpdateTime ?? '',
		})

		const resetState = () => {
			state.activities = []
			state.pageIndex = 0
			state.total = 0
			state.finished = false
		}

		const loadData = async ({ reset = false } = {}) => {
			if (reset) {
				resetState()
			}

			const nextPageIndex = reset ? 1 : state.pageIndex + 1
			state.loading = true

			try {
				const res = await GetPublishedActivityList({
					PageIndex: nextPageIndex,
					PageSize: state.pageSize,
				})
				const data = getResponseData(res)
				const items = Array.isArray(data.items)
					? data.items
					: (Array.isArray(data.Items) ? data.Items : [])
				const list = items.map(normalizeActivity)
				const total = Number(data.totalCount ?? data.TotalCount ?? 0) || 0

				state.pageIndex = nextPageIndex
				state.total = total
				state.activities = reset ? list : state.activities.concat(list)
				state.finished = state.activities.length >= total || list.length < state.pageSize
				state.loaded = true
			} catch (error) {
				const message = error && (error.errorMessage || error.ErrorMessage || error.message)
				state.finished = true
				state.loaded = true
				Toast(message || '活动列表获取失败，请稍后重试')
			} finally {
				state.loading = false
				state.refreshing = false
			}
		}

		const onLoad = () => {
			if (state.loading || state.finished) return
			loadData()
		}

		const onRefresh = () => {
			state.refreshing = true
			loadData({ reset: true })
		}

		const onSelectActivity = (activity) => {
			const id = toText(activity && activity.activityId)
			if (!id) {
				Toast('活动ID缺失，暂时无法进入任务书')
				return
			}
			window.bjsdk.goTo('/activityBrief', {
				append: {
					activityId: id,
				},
			})
		}

		onMounted(() => {
			loadData({ reset: true })
		})

		return {
			isPc,
			contentTop,
			totalText,
			onLoad,
			onRefresh,
			onSelectActivity,
			...toRefs(state),
		}
	},
}
</script>

<style lang="less" scoped>
.page-home {
	min-height: 100vh;
	background:
		radial-gradient(circle at top right, rgba(96, 215, 255, 0.26), transparent 28%),
		linear-gradient(180deg, #f8fbff 0%, #eef3f8 42%, #edf2f7 100%);
	color: #0f172a;

	.page-home-main {
		padding-top: 14px !important;
		padding-bottom: calc(20px + env(safe-area-inset-bottom));
	}

	.activity-section {
		padding: 10px 16px 0;
	}

	.section-head {
		display: flex;
		align-items: flex-end;
		justify-content: space-between;
		gap: 12px;
		margin-bottom: 14px;

		h2 {
			margin: 0;
			font-size: 18px;
			font-weight: 800;
			color: #0f172a;
		}

		p {
			margin: 4px 0 0;
			font-size: 12px;
			line-height: 1.6;
			color: #64748b;
		}
	}

	.section-count {
		flex-shrink: 0;
		padding: 6px 12px;
		border-radius: 999px;
		background: rgba(255, 255, 255, 0.85);
		border: 1px solid #d9e6f5;
		color: #2563eb;
		font-size: 12px;
		font-weight: 700;
	}

	.activity-list {
		display: flex;
		flex-direction: column;
		gap: 14px;
	}
}
</style>
