<template>
	<article class="activity-card" @click="$emit('click', activity)">
		<div class="activity-cover" :style="coverStyle">
			<span class="activity-cover-tag">{{ statusText }}</span>
		</div>

		<div class="activity-body">
			<h3 class="act-title">{{ activity.activityName || '未命名活动' }}</h3>
			<p class="act-core-goal">{{ activity.coreGoal || activity.activityDesc || '暂无活动说明' }}</p>

			<div class="activity-meta">
				<span class="meta-chip">
					<i>时</i>
					{{ durationText }}
				</span>
				<span class="meta-chip">
					<i>期</i>
					{{ timeRangeText }}
				</span>
			</div>

			<div class="act-foot">
				<span>{{ footNoteText }}</span>
				<span class="status-pill" :class="{ done: isFinished }">{{ actionText }}</span>
			</div>
		</div>
	</article>
</template>

<script>
import { computed } from 'vue'
import { utcToLocal } from '@/utils/util'

export default {
	name: 'RoadshowActivityCard',
	props: {
		activity: {
			type: Object,
			default: () => ({}),
		},
	},
	emits: ['click'],
	setup(props) {
		const parseTime = (value) => {
			const text = utcToLocal(value)
			return text || ''
		}

		const formatDate = (value) => {
			const text = parseTime(value)
			if (!text) return ''
			const [datePart] = text.split(' ')
			if (!datePart) return ''
			const parts = datePart.split('-')
			if (parts.length !== 3) return datePart
			return `${parts[1]}-${parts[2]}`
		}

		const getDateMs = (value) => {
			if (!value) return 0
			const text = parseTime(value)
			if (!text) return 0
			const ms = new Date(text.replace(/-/g, '/')).getTime()
			return Number.isFinite(ms) ? ms : 0
		}

		const coverStyle = computed(() => {
			const hasCover = !!props.activity.coverImage
			return {
				backgroundImage: hasCover
					? `linear-gradient(180deg, rgba(15, 23, 42, 0.08), rgba(15, 23, 42, 0.68)), url(${props.activity.coverImage})`
					: 'linear-gradient(135deg, #79bbff 0%, #409eff 35%, #1d4ed8 100%)',
			}
		})

		const durationText = computed(() => {
			const duration = Number(props.activity.recommendedDurationMinutes || 0)
			return duration > 0 ? `${duration} 分钟` : '时长待定'
		})

		const timeRangeText = computed(() => {
			const start = formatDate(props.activity.startTime)
			const end = formatDate(props.activity.endTime)
			if (start && end) return start === end ? start : `${start} - ${end}`
			return start || end || '时间待定'
		})

		const statusText = computed(() => {
			const now = Date.now()
			const start = getDateMs(props.activity.startTime)
			const end = getDateMs(props.activity.endTime)
			if (end && now > end) return '已结束'
			if (start && now < start) return '即将开始'
			return '进行中'
		})

		const isFinished = computed(() => statusText.value === '已结束')
		const actionText = computed(() => (isFinished.value ? '查看信息' : '进入活动'))
		const footNoteText = computed(() => props.activity.customerBackground || props.activity.activityDesc || '点击查看活动详情')

		return {
			coverStyle,
			durationText,
			timeRangeText,
			statusText,
			isFinished,
			actionText,
			footNoteText,
		}
	},
}
</script>

<style lang="less" scoped>
.activity-card {
	overflow: hidden;
	border-radius: 20px;
	background: #ffffff;
	border: 1px solid #e2e8f0;
	box-shadow: 0 4px 16px rgba(15, 23, 42, 0.03);
	cursor: pointer;
	transition: all 0.2s ease;

	&:active {
		transform: scale(0.985);
		background: #f8fafc;
	}
}

.activity-cover {
	position: relative;
	display: flex;
	align-items: flex-end;
	height: 110px;
	padding: 14px;
	background-size: cover;
	background-position: center;
}

.activity-cover-tag {
	position: relative;
	z-index: 1;
	display: inline-flex;
	align-items: center;
	padding: 3px 8px;
	border-radius: 6px;
	background: rgba(255, 255, 255, 0.2);
	border: 1px solid rgba(255, 255, 255, 0.3);
	backdrop-filter: blur(4px);
	color: #ffffff;
	font-size: 11px;
	font-weight: 700;
}

.activity-body {
	padding: 14px 16px 15px;
}

.act-title {
	margin: 0 0 8px;
	font-size: 15.5px;
	line-height: 1.35;
	font-weight: 800;
	color: #0f172a;
}

.act-core-goal {
	margin: 0 0 12px;
	font-size: 12.5px;
	line-height: 1.65;
	color: #475569;
	text-align: justify;
	display: -webkit-box;
	-webkit-line-clamp: 2;
	-webkit-box-orient: vertical;
	overflow: hidden;
}

.activity-meta {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 10px;
}

.meta-chip {
	display: inline-flex;
	align-items: center;
	gap: 5px;
	min-height: 28px;
	padding: 5px 9px;
	border-radius: 999px;
	background: #f8fafc;
	border: 1px solid #e2e8f0;
	color: #475569;
	font-size: 12px;
	font-weight: 700;

	i {
		font-style: normal;
		color: #409eff;
		font-size: 13px;
	}
}

.act-foot {
	display: flex;
	align-items: center;
	justify-content: space-between;
	gap: 12px;
	padding-top: 10px;
	margin-top: 10px;
	border-top: 1px solid #f1f5f9;
	font-size: 12px;
	color: #64748b;

	span:first-child {
		flex: 1;
		min-width: 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}
}

.status-pill {
	flex-shrink: 0;
	padding: 5px 10px;
	border-radius: 999px;
	background: #ecf5ff;
	color: #409eff;
	font-size: 11px;
	font-weight: 800;

	&.done {
		background: #dcfce7;
		color: #15803d;
	}
}
</style>
