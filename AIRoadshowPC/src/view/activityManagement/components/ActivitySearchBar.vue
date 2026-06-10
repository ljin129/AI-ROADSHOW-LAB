<template>
	<div class="toolbar">
		<div class="search-grid">
			<el-input
				v-model.trim="searchForm.keyword"
				placeholder="请输入活动名称或活动描述关键字"
				clearable
				size="small"
			/>
			<el-date-picker
				v-model="searchForm.dateRange"
				type="datetimerange"
				range-separator="至"
				start-placeholder="开始时间"
				end-placeholder="结束时间"
				value-format="yyyy-MM-dd HH:mm:ss"
				size="small"
			/>
			<el-select v-model="searchForm.status" placeholder="活动状态" clearable size="small">
				<el-option label="未发布" value="未发布" />
				<el-option label="已发布" value="已发布" />
			</el-select>
			<div class="toolbar-buttons">
				<el-button type="primary" icon="el-icon-search" size="small" @click="$emit('search')">查询</el-button>
				<el-button size="small" @click="$emit('reset')">重置</el-button>
			</div>
		</div>
		<el-button type="primary" icon="el-icon-plus" size="small" @click="$emit('create')">新增活动</el-button>
	</div>
</template>

<script>
export default {
	name: 'ActivitySearchBar',
	props: {
		searchForm: {
			type: Object,
			required: true,
		},
	},
}
</script>

<style scoped>
.toolbar {
	display: flex;
	align-items: flex-start;
	justify-content: space-between;
	gap: 16px;
	margin-bottom: 18px;
}

.search-grid {
	flex: 1;
	display: grid;
	grid-template-columns: minmax(220px, 260px) minmax(320px, 1fr) 140px auto;
	gap: 12px;
}

.toolbar-buttons {
	display: flex;
	gap: 8px;
}

@media (max-width: 1180px) {
	.toolbar {
		flex-direction: column;
	}

	.search-grid {
		width: 100%;
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}
}

@media (max-width: 720px) {
	.search-grid {
		grid-template-columns: 1fr;
	}
}
</style>
