const cloneDeep = (value) => JSON.parse(JSON.stringify(value))

export const dimensionOptions = [
	{ id: 'dim-1', name: '需求洞察' },
	{ id: 'dim-2', name: '方案表达' },
	{ id: 'dim-3', name: '异议处理' },
	{ id: 'dim-4', name: '关系推进' },
]

export const createDefaultActivityForm = () => ({
	name: '',
	desc: '',
	coverImage: '',
	background: '',
	time: [],
	duration: 30,
	published: false,
	backgroundAttachments: [],
	questionAttachments: [],
})

export const createEmptyDraft = () => ({
	form: createDefaultActivityForm(),
	roles: [],
	scenes: [],
	questions: [],
})

const createRole = (id, name, persona, goal, desc) => ({
	id,
	name,
	persona,
	goal,
	desc,
})

const createScene = (id, name, objective, desc) => ({
	id,
	name,
	objective,
	desc,
})

const createQuestion = (id, content, sceneId, dimensionId, type, difficulty) => ({
	id,
	content,
	sceneId,
	dimensionId,
	type,
	difficulty,
})

export const createGeneratedDraft = () => ({
	form: {
		name: '政企客户路演能力提升活动',
		desc: '围绕客户需求洞察、方案表达和异议处理开展的静态示例活动。',
		background:
			'客户为区域重点政企单位，正在推进数字化升级，期望通过能力测评和情景演练识别销售团队在路演过程中的能力短板。',
		time: ['2026-06-10 09:00', '2026-06-18 18:00'],
		duration: 45,
		published: false,
		backgroundAttachments: [],
		questionAttachments: [],
	},
	roles: [
		createRole(
			'role-1',
			'客户经理',
			'负责前期需求梳理与路演主讲',
			'在正式路演中清晰传达解决方案价值',
			'重点考察需求洞察、表达结构与现场掌控能力。'
		),
		createRole(
			'role-2',
			'客户决策人',
			'关注投资回报与实施风险',
			'评估方案是否值得纳入年度预算',
			'通过关键追问观察学员对业务价值和风险的回应能力。'
		),
	],
	scenes: [
		createScene(
			'scene-1',
			'项目立项沟通',
			'识别客户真实诉求并建立高层认可',
			'聚焦客户背景梳理、痛点挖掘和立项驱动因素确认。'
		),
		createScene(
			'scene-2',
			'方案汇报答辩',
			'完成方案汇报并有效应对异议',
			'模拟客户现场追问预算、周期和实施可行性等关键问题。'
		),
	],
	questions: [
		createQuestion(
			'question-1',
			'请用三分钟说明你对客户当前数字化建设现状的理解，以及你判断最值得优先解决的问题。',
			'scene-1',
			'dim-1',
			'开放问答',
			'中'
		),
		createQuestion(
			'question-2',
			'如果客户质疑项目收益难以量化，你会如何重新组织你的表达框架？',
			'scene-2',
			'dim-2',
			'追问题',
			'高'
		),
		createQuestion(
			'question-3',
			'当客户提出“预算不足，先观望”的时候，你会如何回应并推进下一步？',
			'scene-2',
			'dim-3',
			'情景题',
			'高'
		),
	],
})

export const createMockActivities = () => {
	const draftA = createGeneratedDraft()
	draftA.form.published = true
	draftA.form.name = '数字政府项目路演专场'
	draftA.form.desc = '面向政企客户经理的综合路演演练活动。'
	draftA.form.time = ['2026-06-01 09:00', '2026-06-08 18:00']

	const draftB = createGeneratedDraft()
	draftB.form.name = '制造业客户升级路演'
	draftB.form.desc = '聚焦制造业客户的需求诊断与解决方案表达。'
	draftB.form.time = ['2026-06-12 10:00', '2026-06-20 17:30']

	const draftC = createGeneratedDraft()
	draftC.form.name = '重点客户汇报陪练营'
	draftC.form.desc = '覆盖汇报结构、业务价值证明与关系推进。'
	draftC.form.time = ['2026-06-21 09:30', '2026-06-28 17:00']

	return [
		createActivityRecord(1, draftA),
		createActivityRecord(2, draftB),
		createActivityRecord(3, draftC),
	]
}

export const createActivityRecord = (id, draft) => ({
	id,
	name: draft.form.name,
	desc: draft.form.desc,
	coverImage: draft.form.coverImage || '',
	start: draft.form.time[0] || '',
	end: draft.form.time[1] || '',
	status: draft.form.published ? '已发布' : '未发布',
	config: cloneDeep(draft),
})

export const cloneDraft = (draft) => cloneDeep(draft)

export const createDraftFromActivity = (activity = {}) => ({
	form: {
		name: activity.name || '',
		desc: activity.desc || '',
		coverImage: activity.coverImage || '',
		background: activity.background || '',
		time: [activity.start || '', activity.end || ''].filter(Boolean),
		duration: activity.duration || 30,
		published: activity.publishStatus === 1 || activity.published === true,
		backgroundAttachments: activity.backgroundAttachments || [],
		questionAttachments: activity.questionAttachments || [],
	},
	roles: cloneDeep(activity.roles || []),
	scenes: cloneDeep(activity.scenes || []),
	questions: cloneDeep(activity.questions || []),
})
