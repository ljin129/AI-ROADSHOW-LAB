<template>
	<el-table :data="activities" border class="activity-table">
		<el-table-column prop="name" label="活动名称" min-width="220" />
		<el-table-column label="封面图" width="140" align="center">
			<template slot-scope="scope">
				<el-image
					v-if="scope.row.coverImage"
					:src="getCoverImageUrl(scope.row.coverImage)"
					:preview-src-list="[getCoverImageUrl(scope.row.coverImage)]"
					fit="cover"
					class="cover-image"
				/>
				<span v-else class="empty-text">-</span>
			</template>
		</el-table-column>
		<el-table-column prop="desc" label="活动描述" min-width="280" show-overflow-tooltip />
		<el-table-column prop="start" label="开始时间" width="170" align="center" />
		<el-table-column prop="end" label="结束时间" width="170" align="center" />
		<el-table-column label="发布状态" width="110" align="center">
			<template slot-scope="scope">
				<el-tag :type="scope.row.status === '已发布' ? 'success' : 'info'" size="small">
					{{ scope.row.status }}
				</el-tag>
			</template>
		</el-table-column>
		<el-table-column label="操作" width="140" fixed="right" align="center">
			<template slot-scope="scope">
				<el-button type="text" @click="$emit('edit', scope.row)">编辑</el-button>
				<el-button type="text" class="danger-text" @click="$emit('delete', scope.row)">删除</el-button>
			</template>
		</el-table-column>
	</el-table>
</template>

<script>
import { getFullUrl } from '@/utils'

export default {
	name: 'ActivityTable',
	props: {
		activities: {
			type: Array,
			default: function () {
				return []
			},
		},
	},
	methods: {
		getCoverImageUrl(coverImage) {
			return getFullUrl(coverImage, 240)
		},
	},
}
</script>

<style scoped>
.activity-table {
	width: 100%;
}

.cover-image {
	width: 88px;
	height: 56px;
	border-radius: 8px;
	background: #f5f7fa;
}

.empty-text {
	color: #c0c4cc;
}

.danger-text {
	color: #f56c6c;
}
</style>
