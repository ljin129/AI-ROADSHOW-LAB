<template>
	<div class="empty-box">
		<div class="empty-icon"></div>
		<h3>{{ title }}</h3>
		<p>{{ description }}</p>
	</div>
</template>

<script>
export default {
	name: 'RoadshowEmpty',
	props: {
		title: {
			type: String,
			default: '暂无数据',
		},
		description: {
			type: String,
			default: '请稍后再试',
		},
	},
}
</script>

<style lang="less" scoped>
.empty-box {
	margin-top: 24px;
	padding: 36px 20px;
	border-radius: 20px;
	background: rgba(255, 255, 255, 0.82);
	border: 1px solid #e2e8f0;
	text-align: center;
	box-shadow: 0 8px 24px rgba(15, 23, 42, 0.04);

	h3 {
		margin: 0;
		font-size: 16px;
		font-weight: 800;
		color: #0f172a;
	}

	p {
		margin: 8px 0 0;
		font-size: 12px;
		line-height: 1.7;
		color: #64748b;
	}
}

.empty-icon {
	width: 68px;
	height: 68px;
	margin: 0 auto 14px;
	border-radius: 22px;
	background:
		radial-gradient(circle at 30% 30%, #ffffff 0%, rgba(255, 255, 255, 0.42) 22%, transparent 23%),
		linear-gradient(135deg, #dbeafe 0%, #bfdbfe 50%, #93c5fd 100%);
	box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.75);
}
</style>
