const activityManagementRoute = [
	{
		path: '/activityManagement',
		name: 'ActivityManagement',
		meta: {
			keepAlive: false,
		},
		component: () => import('@/view/activityManagement/index.vue'),
	},
]

export default activityManagementRoute
