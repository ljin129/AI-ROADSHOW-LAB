<template>
	<router-view v-slot="{ Component }">
		<keep-alive :include="includes">
			<component :is="Component" :key="keepAlive ? 'keepKey' : keepKey"/>
		</keep-alive>
	</router-view>
	<!-- <bottom-com v-if="!hideTab && ['/home','/mine'].includes(keepKey)"></bottom-com> -->
</template>
<script>
import { computed, onMounted,ref,watchEffect } from 'vue'
import { useRoute } from 'vue-router'
import router from '@/router/index.js'
export default {
	setup() {
		const route = useRoute()
		const includes = []
		const routers = router.getRoutes()
		routers.forEach(r => {
			if (r.meta?.keepAlive) {
				includes.push(r.name)
			}
		});
		console.log('router', includes)
		// 路由缓存的key
		// const keepKey = computed(() => route.path)
		const keepKey = ref(route.path || 'keepKey');
		const customerNav = computed(() => route.meta.customerNav);

		watchEffect(() => {
			keepKey.value = route.path;
			console.log('keepKey=',keepKey.value, keepKey.value=='/home');
		});

		// 查看路由整个页面是否需要缓存
		const keepAlive = computed(() => route.meta.keepAlive);

		onMounted(() => {
			
		})
		return {
			keepKey,
			keepAlive,
			hideTab:window.bjsdk.query('hideTab')==1 || window.__wxjs_environment === 'miniprogram',//是否在微信小程序中
			includes,
		}
	},
}
</script>
<style lang="less">

</style>
