const routes = [
	{
		path: '/home',
		name: 'Home',
		component: () =>
			import(/* webpackChunkName: "Home" */ '@/views/home/index.vue'),
		meta: {
			keepAlive: false,
			title: 'AI路演智能演练营',
		},
	},
	{
		path: '/activityBrief',
		name: 'ActivityBrief',
		component: () =>
			import(/* webpackChunkName: "ActivityBrief" */ '@/views/home/activityBrief.vue'),
		meta: {
			keepAlive: false,
			title: '演练任务书',
		},
	},
	{
		path: '/activityCombat',
		name: 'ActivityCombat',
		component: () =>
			import(/* webpackChunkName: "ActivityCombat" */ '@/views/home/activityCombat.vue'),
		meta: {
			keepAlive: false,
			title: '实战交锋中',
		},
	},
]

export default routes
