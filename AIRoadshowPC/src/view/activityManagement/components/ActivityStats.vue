<template>
	<div class="stats-grid">
		<div v-for="item in stats" :key="item.label" class="stat-card" :class="item.type">
			<div class="stat-label">{{ item.label }}</div>
			<div class="stat-value">{{ item.value }}</div>
		</div>
	</div>
</template>

<script>
export default {
	name: 'ActivityStats',
	props: {
		stats: {
			type: Array,
			default: function () {
				return []
			},
		},
	},
}
</script>

<style scoped>
.stats-grid {
	display: grid;
	grid-template-columns: repeat(4, minmax(0, 1fr));
	gap: 16px;
	margin-bottom: 20px;
}

.stat-card {
	padding: 20px 22px;
	border-radius: 18px;
	background: linear-gradient(180deg, #ffffff 0%, #f7faff 100%);
	box-shadow: 0 14px 30px rgba(31, 45, 61, 0.08);
	border: 1px solid rgba(64, 158, 255, 0.08);
}

.stat-card.primary {
	background: linear-gradient(135deg, #eef6ff 0%, #dcecff 100%);
}

.stat-card.success {
	background: linear-gradient(135deg, #effcf4 0%, #d7f3e3 100%);
}

.stat-label {
	font-size: 13px;
	color: #6b778c;
}

.stat-value {
	margin-top: 10px;
	font-size: 32px;
	line-height: 1;
	font-weight: 700;
	color: #1f2d3d;
}

@media (max-width: 1080px) {
	.stats-grid {
		grid-template-columns: repeat(2, minmax(0, 1fr));
	}
}

@media (max-width: 640px) {
	.stats-grid {
		grid-template-columns: 1fr;
	}
}
</style>
