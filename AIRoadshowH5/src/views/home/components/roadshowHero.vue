<template>
	<section class="top-hero">
		<div class="page-kicker">AI ROADSHOW LAB</div>
		<h1 class="page-title">AI路演实战模拟舱</h1>
		<p class="page-subtitle">
			聚焦真实业务场景，开启全真沉浸式路演演练。一键直达活动现场，在实战中淬炼表达，复盘成长。
		</p>
	</section>
</template>

<script>
export default {
	name: 'RoadshowHero',
}
</script>

<style lang="less" scoped>
.top-hero {
	position: relative;
	overflow: hidden;
	margin: 0 16px 18px;
	padding: 20px;
	border-radius: 20px;
	background: linear-gradient(135deg, #ffffff 0%, #eef7ff 58%, #dff0ff 100%);
	border: 1px solid rgba(64, 158, 255, 0.16);
	box-shadow: 0 10px 26px rgba(64, 158, 255, 0.1);

	&::after {
		content: '';
		position: absolute;
		right: -36px;
		top: -44px;
		width: 128px;
		height: 128px;
		border-radius: 50%;
		background: rgba(64, 158, 255, 0.13);
	}
}

.page-kicker {
	position: relative;
	z-index: 1;
	font-size: 10px;
	font-weight: 800;
	letter-spacing: 1.5px;
	text-transform: uppercase;
	color: #409eff;
}

.page-title {
	position: relative;
	z-index: 1;
	margin: 6px 0 4px;
	font-size: 22px;
	line-height: 1.2;
	font-weight: 800;
	color: #0f172a;
}

.page-subtitle {
	position: relative;
	z-index: 1;
	margin: 0;
	font-size: 12px;
	line-height: 1.7;
	color: #64748b;
}
</style>
