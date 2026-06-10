const abilityDimensionRoute = [
	{
		path: '/abilityDimension',
		name: 'AbilityDimension',
		meta: {
			keepAlive: false,
		},
		component: () =>
			import(
				/* webpackChunkName: "Beta.AIRoadshowPC_abilityDimension" */ '@/view/abilityDimension/index.vue'
			),
	},
]

export default abilityDimensionRoute